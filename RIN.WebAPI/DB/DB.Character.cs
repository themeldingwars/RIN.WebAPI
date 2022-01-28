﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProtoBuf;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Models.Common;
using RIN.WebAPI.Models.DB;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.DB
{
    public partial class DB
    {
        public async Task<long> CreateNewCharacter(long accountId, string name, bool isDev, int voiceSetId, int gender, int current_battleframe_id, byte[] visualsBlob)
        {
            var result = await DBCall(async conn =>
            {
                var p = new DynamicParameters();
                p.Add("@account_id", accountId);
                p.Add("@name", name);
                p.Add("@is_dev", isDev);
                p.Add("@voice_setid", voiceSetId);
                p.Add("@gender", gender);
                p.Add("@current_battleframe_id", current_battleframe_id);
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
                            character_guid,
                            name,
                            unique_name,
                            is_dev,
                            is_active,
                            created_at,
                            title_id,
                            time_played_secs,
                            needs_name_change,
                            1 AS max_frame_level,
                            current_battleframe_id AS frame_sdb_id,
                            1 AS current_level,
                            gender,
                            0 AS elite_rank,
                            last_seen_at,
                            visuals,
                            race,
                            deleted_at,
                            expires_in
                            FROM webapi.""Characters""
                            WHERE account_id = @accountId";

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

        // TODO: Check if character is an army commander and prevent delete process if true
        public async Task<Error> DeleteCharacterById(long accountId, long characterGuid)
        {
            const string SELECT_SQL = @"SELECT 
                            character_guid,
                            deleted_at
                            FROM webapi.""Characters""
                            WHERE account_id = @accountId
                            AND character_guid = @characterGuid";

            var select_results = await DBCall(async conn => conn.Query<dynamic>(SELECT_SQL, new { accountId, characterGuid }));

            if (select_results.Count() == 1)
            {
                if (select_results.First().deleted_at != null)
                {
                    return new Error() { code = Error.Codes.ERR_CHAR_DELETED, message = "Character is already marked as deleted" };
                }

                DateTime deleted_at = DateTime.Now;
                DateTime expires_in = deleted_at.AddMonths(1);

                const string UPDATE_SQL = @"UPDATE
                            webapi.""Characters""
                            SET deleted_at = @deleted_at,
                            expires_in = @expires_in
                            WHERE account_id = @accountId
                            AND character_guid = @characterGuid";

                var update_results = await DBCall(async conn => conn.Query<dynamic>(UPDATE_SQL, new { accountId, characterGuid, deleted_at, expires_in }));

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
            else
            {
                return new Error() { code = Error.Codes.ERR_CHAR_NOT_FOUND, message = "Can't find a character with that GUID" };
            }
        }

        public async Task<Error> UndeleteCharacterById(long accountId, long characterGuid)
        {
            const string SELECT_SQL = @"SELECT 
                            character_guid
                            FROM webapi.""Characters""
                            WHERE account_id = @accountId
                            AND character_guid = @characterGuid";

            var select_results = await DBCall(async conn => conn.Query<dynamic>(SELECT_SQL, new { accountId, characterGuid }));

            if (select_results.Count() == 1)
            {
                const string UPDATE_SQL = @"UPDATE
                            webapi.""Characters""
                            SET deleted_at = NULL,
                            expires_in = NULL
                            WHERE account_id = @accountId
                            AND character_guid = @characterGuid";

                var update_results = await DBCall(async conn => conn.Query<dynamic>(UPDATE_SQL, new { accountId, characterGuid }));

                return new Error() { code = Error.Codes.SUCCESS };
            }
            else
            {
                return new Error() { code = Error.Codes.ERR_CHAR_NOT_FOUND, message = "Can't find a character with that GUID" };
            }
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
    }
}