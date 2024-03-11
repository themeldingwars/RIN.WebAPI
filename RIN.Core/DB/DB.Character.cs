using System.Data;
using Dapper;
using ProtoBuf;
using RIN.Core.ClientApi;
using RIN.Core.Common;
using RIN.Core.Models;
using RIN.Core.Models.ClientApi;
using RIN.Core.Utils;

namespace RIN.Core.DB
{
    public partial class DB
    {
        public async Task<long> CreateNewCharacter(long accountId, string name, bool isDev, int voiceSetId, int gender, byte[] visualsBlob)
        {
            var result = await DBCall(async conn =>
            {
                var p = new DynamicParameters();
                p.Add("@account_id", accountId);
                p.Add("@name", name);
                p.Add("@is_dev", isDev);
                p.Add("@voice_setid", voiceSetId);
                p.Add("@gender", gender);
                p.Add("@visuals", visualsBlob);

                p.Add("@error_text", dbType: DbType.String, direction: ParameterDirection.Output);
                p.Add("@new_character_id", dbType: DbType.Int64, direction: ParameterDirection.Output);

                var r = await conn.ExecuteAsync("webapi.\"CreateNewCharacter\"", p, commandType: CommandType.StoredProcedure);

                return (p.Get<long>("@new_character_id"), p.Get<string>("@error_text"));
            });

            if (result.Item1 == -1) {
                throw new TmwException(result.Item2, result.Item2);
            }

            return result.Item1;
        }

        public async Task<List<Character>> GetCharactersForAccount(long accountId)
        {
            const string SELECT_SQL = @"SELECT 
		                        webapi.""Characters"".character_guid,
		                        name,
		                        unique_name,
		                        is_dev,
		                        is_active,
		                        created_at,
		                        title_id,
		                        time_played_secs,
		                        needs_name_change,
		                        (SELECT MAX(level) FROM webapi.""Battleframes"" WHERE character_guid = webapi.""Characters"".character_guid) AS max_frame_level,
		                        Battleframes.battleframe_sdb_id AS frame_sdb_id,
		                        Battleframes.level AS current_level,
		                        gender,
		                        0 AS elite_rank,
		                        last_seen_at,
		                        webapi.""Characters"".visuals,
		                        race,
		                        DeletionQueue.deleted_at,
		                        DeletionQueue.expires_in
		                        FROM webapi.""Characters""
				                        LEFT JOIN
					                        webapi.""DeletionQueue"" as DeletionQueue
						                        ON DeletionQueue.character_guid = webapi.""Characters"".character_guid
						
				                        LEFT JOIN
					                        webapi.""Battleframes"" as Battleframes
						                        ON Battleframes.id = webapi.""Characters"".current_battleframe_guid
		                        WHERE webapi.""Characters"".account_id = @accountId";

            var results = await DBCall(async conn => conn.Query<dynamic>(SELECT_SQL, new {accountId}));

            var chars = new List<Character>(results.Count());
            foreach (var result in results)
            {
                long? deleted_at = result.deleted_at == null ? null : ((DateTimeOffset)result.deleted_at).ToUnixTimeSeconds();
                long? expires_in = result.expires_in == null ? null : ((DateTimeOffset)result.expires_in).ToUnixTimeSeconds() - DateTimeOffset.Now.ToUnixTimeSeconds();

                var character = new Character
                {
                    character_guid    = result.character_guid,
                    name              = result.name,
                    unique_name       = result.unique_name,
                    is_dev            = result.is_dev,
                    is_active         = result.is_active,
                    created_at        = result.created_at,
                    title_id          = result.title_id,
                    time_played_secs  = result.time_played_secs ?? 0,
                    needs_name_change = result.needs_name_change,
                    max_frame_level   = result.max_frame_level,
                    frame_sdb_id      = result.frame_sdb_id,
                    current_level     = result.current_level,
                    gender            = result.gender,
                    current_gender    = CharacterUtil.GenderNumToString(result.gender),
                    elite_rank        = result.elite_rank,
                    last_seen_at      = result.last_seen_at,
                    gear              = new List<GearSlot>(),
                    expires_in        = expires_in,
                    deleted_at        = deleted_at,
                    race              = CharacterUtil.RaceIdToString(result.race),
                    migrations        = new List<int>()
                };

                if ((result.visuals as byte[]).Length > 0) {
                    var charaterVisuals = Serializer.Deserialize<CharacterVisuals>((result.visuals as byte[]).AsSpan());
                    
                    charaterVisuals.ornaments ??= new List<WebId>();

                    character.visuals = new CharacterBattleframeCombinedVisuals();
                    charaterVisuals.ApplyToCharacterVisuals(character.visuals);

                    var defaultBattleframeVisuals = PlayerBattleframeVisuals.CreateDefault();
                    defaultBattleframeVisuals.ApplyToCharacterVisuals(character.visuals);
                }

                chars.Add(character);
            }

            return chars;
        }

        public async Task<(BasicCharacterInfo info, CharacterVisuals visuals)> GetBasicCharacterAndVisualData(long charId)
        {
            const string SELECT_SQL = @"SELECT c.name, title_id, gender, race, current_battleframe_guid, bf.battleframe_sdb_id AS CurrentBattleframeSDBId, 
                                a.tag as ArmyTag, a.army_guid as ArmyGUID, ar.is_officer as ArmyIsOfficer, c.visuals
	                        FROM webapi.""Characters"" as c
                                LEFT JOIN
					                webapi.""Battleframes"" as bf
						                ON bf.id = c.current_battleframe_guid
	                            LEFT JOIN webapi.""ArmyMembers"" as am 
	                                    ON c.character_guid = am.character_guid
	                            LEFT JOIN webapi.""Armies"" as a 
	                                    ON am.army_guid = a.army_guid
	                            LEFT JOIN webapi.""ArmyRanks"" as ar
	                                    ON am.army_rank_id = ar.army_rank_id
	                        WHERE c.character_guid = @charId";

            var result = await DBCall(async conn => conn.Query<BasicCharacterInfo, byte[], (BasicCharacterInfo, CharacterVisuals)>(
                SELECT_SQL,
                map: (charinfo, visuals) =>
                {
                    var parsedVisuals = Utils.MiscUtils.FromProtoBuffByteArray<CharacterVisuals>(visuals.AsSpan()) ?? new CharacterVisuals();
                    return (charinfo, parsedVisuals);
                },
                splitOn: "visuals",
                param: new { charId })
            .Single());

            return result;
        }

        public async Task<bool> UpdateCharacterVisuals(long charId, CharacterVisuals visuals)
        {
            const string UPDATE_SQL = @"UPDATE webapi.""Characters""
	                            SET gender = @gender, race = @race, visuals = @visualsBlob
	                            WHERE character_guid = @charId;";

            var visualsBlob = Utils.MiscUtils.ToProtoBuffByteArray(visuals);
            var result = await DBCall(conn => conn.ExecuteAsync(UPDATE_SQL, new { charId, visuals.gender, visuals.race, visualsBlob }));

            return result > 0;
        }

        // Set this charaters currently equiped battleframe
        public async Task<bool> SetCharacterCurrentBattleframe(long charId, long bfId)
        {
            const string UPDATE_SQL = @"UPDATE webapi.""Characters""
	                            SET current_battleframe_guid = @bfId
	                            WHERE character_guid = @charId;";

            var result = await DBCall(conn => conn.ExecuteAsync(UPDATE_SQL, new { charId, bfId }));

            return result > 0;
        }

        public async Task<Error> SetPendingDeleteCharacterById(long accountId, long characterGuid)
        {
            // TODO: Move calls to be inside the database to make a single DB call

            // Check if character exists and is owned by the account
            const string CHAR_SELECT_SQL = @"SELECT 
                            character_guid
                            FROM webapi.""Characters""
                            WHERE account_id = @accountId
                            AND character_guid = @characterGuid";

            var char_select_results = await DBCall(async conn => conn.Query<dynamic>(CHAR_SELECT_SQL, new { accountId, characterGuid }));

            if (char_select_results.Count() == 0)
            {
                return new Error() { code = Error.Codes.ERR_CHAR_NOT_FOUND, message = "Can't find a character with that GUID" };
            }

            // Check if character is an army commander and prevent delete process if true
            const string IS_COMMANDER_SQL = @"SELECT 
                            character_guid
                            FROM webapi.""ArmyMembers"" am
                            INNER JOIN webapi.""ArmyRanks"" ar on ar.army_rank_id = am.army_rank_id
                            WHERE am.character_guid = @characterGuid AND ar.is_commander = true";

            var isCommanderResults = await DBCall(
                async conn => await conn.QueryAsync<dynamic>(IS_COMMANDER_SQL, new { characterGuid })
            );

            if (isCommanderResults.Any())
            {
                return new Error() { code = Error.Codes.ERR_CANNOT_DELETE_COMMANDER, message = "Character is an army commander" };
            }

            // Check if character is already marked for deletion
            const string SELECT_SQL = @"SELECT 
                            character_guid,
                            deleted_at
                            FROM webapi.""DeletionQueue""
                            WHERE account_id = @accountId
                            AND character_guid = @characterGuid";

            var select_results = await DBCall(async conn => conn.Query<dynamic>(SELECT_SQL, new { accountId, characterGuid }));

            if (select_results.Count() == 1)
            {
                return new Error() { code = Error.Codes.ERR_CHAR_DELETED, message = "Character is already marked as deleted" };
            }

            DateTime deleted_at = DateTime.Now;
            DateTime expires_in = deleted_at.AddMonths(1);

            const string INSERT_SQL = @"INSERT
                        INTO webapi.""DeletionQueue""
                        (character_guid, account_id, deleted_at, expires_in)
                        VALUES (@characterGuid, @accountId, @deleted_at, @expires_in)";

            var insert_results = await DBCall(async conn => conn.Query<dynamic>(INSERT_SQL, new { characterGuid, accountId, deleted_at, expires_in }));

            // Uncomment to instantly delete character instead of waiting (for dev-use only)
            /*
            const string DELETE_SQL = @"DELETE 
                        FROM webapi.""Characters"" 
                        WHERE account_id = @accountId
                        AND character_guid = @characterGuid";

            var delete_results = await DBCall(async conn => conn.Query<dynamic>(DELETE_SQL, new { accountId, characterGuid }));
            */

            return new Error() { code = Error.Codes.SUCCESS };
        }

        public async Task<Error> UndeleteCharacterById(long accountId, long characterGuid)
        {
            // TODO: Potentially move this function into the database to reduce DB calls

            // Check if character exists and is owned by the account
            const string SELECT_SQL = @"SELECT 
                            webapi.""Characters"".character_guid,
                            DeletionQueue.deleted_at,
                            DeletionQueue.expires_in
                            FROM webapi.""Characters""
                            LEFT JOIN
                            webapi.""DeletionQueue"" as DeletionQueue
                            ON DeletionQueue.character_guid = webapi.""Characters"".character_guid
                            WHERE webapi.""Characters"".account_id = @accountId
                            AND webapi.""Characters"".character_guid = @characterGuid";

            var select_results = await DBCall(async conn => conn.Query<dynamic>(SELECT_SQL, new { accountId, characterGuid }));

            if (select_results.Count() == 0)
            {
                return new Error() { code = Error.Codes.ERR_CHAR_NOT_FOUND, message = "Can't find a character with that GUID" };
            }

            const string DELETE_SQL = @"DELETE
                            FROM webapi.""DeletionQueue""
                            WHERE account_id = @accountId
                            AND character_guid = @characterGuid";

            var delete_results = await DBCall(async conn => conn.Query<dynamic>(DELETE_SQL, new { accountId, characterGuid }));

            return new Error() { code = Error.Codes.SUCCESS };
        }

        public async Task<bool> CheckIfNameIsFree(string name)
        {
            string unique_name = name.ToUpper();
            const string SELECT_SQL = @"SELECT 
                            name
                            FROM webapi.""Characters""
                            WHERE name = @name
                            OR unique_name = @unique_name";

            var select_results = await DBCall(async conn => conn.Query<dynamic>(SELECT_SQL, new { name, unique_name }));

            if (select_results.Count() == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public async Task<bool> IsMemberOfArmy(long characterGuid, long? armyGuid = null)
        { 
            string selectSql = @"
                SELECT character_guid
                FROM webapi.""ArmyMembers""
                WHERE character_guid = @characterGuid";

            if (armyGuid != null)
            {
                selectSql += " AND army_guid = @armyGuid";
            }

            var results = await DBCall(async conn => await conn.QueryAsync<dynamic>(selectSql, new { characterGuid, armyGuid }));

            return results.Any();
        }

        public async Task<IEnumerable<ArmyApplication>> GetPersonalArmyApplications(long characterGuid)
        {
            const string SELECT_SQL = @"
                SELECT id, a.name AS army_name, army_guid, character_guid, message, 'apply' as direction 
                FROM webapi.""ArmyApplications"" aa
                INNER JOIN webapi.""Armies"" a USING(army_guid)
                WHERE character_guid = @characterGuid AND inviter_guid IS NULL";

            return await DBCall(async conn => await conn.QueryAsync<ArmyApplication>(SELECT_SQL, new {characterGuid}));
        }

        public async Task<IEnumerable<ArmyApplication>> GetPersonalArmyInvites(long characterGuid)
        {
            const string SELECT_SQL = @"
                SELECT id, a.name AS army_name, army_guid, character_guid, message, 'invite' AS direction 
                FROM webapi.""ArmyApplications"" aa
                INNER JOIN webapi.""Armies"" a USING(army_guid)
                WHERE character_guid = @characterGuid AND inviter_guid IS NOT NULL";

            var results = await DBCall(async conn => await conn.QueryAsync<ArmyApplication>(SELECT_SQL, new {characterGuid}));

            return results;
        }
    }
}
