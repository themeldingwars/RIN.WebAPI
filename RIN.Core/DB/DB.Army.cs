using System.Data;
using System.Transactions;
using Dapper;
using RIN.Core.ClientApi;
using RIN.Core.Models.ClientApi;
using RIN.WebAPI.Models.ClientApi;

namespace RIN.Core.DB
{
    public partial class DB
    {
        public async Task<PageResults<ArmyListItem>> GetArmies(int page, int perPage, string searchQuery)
        {
            const string SELECT_SQL = @"
                SELECT COUNT(*) AS total_count 
                FROM webapi.""Armies"" a
                WHERE @searchQuery IS NULL OR name ILIKE @searchQuery;

                SELECT 
                    army_guid,
                    name,
                    personality,
                    is_recruiting,
                    region,
                    (SELECT COUNT(*) FROM webapi.""ArmyMembers"" am WHERE am.army_guid = a.army_guid) AS member_count
                FROM webapi.""Armies"" a
                WHERE @searchQuery IS NULL OR name ILIKE @searchQuery
                ORDER BY LOWER(name) ASC 
                LIMIT @perPage OFFSET @offset";

            var p = new DynamicParameters();
            p.Add(
                "searchQuery",
                dbType: DbType.String,
                value: string.IsNullOrEmpty(searchQuery) ? null : $"%{searchQuery}%"
            );
            p.Add("perPage", perPage);
            p.Add("offset", (page - 1) * perPage);


            var pageResults = await DBCall(
                async conn =>
                {
                    await using var multiQuery = await conn.QueryMultipleAsync(SELECT_SQL, p);

                    var pageResults = multiQuery.Read<PageResults<ArmyListItem>>().Single();
                    pageResults.page = page.ToString();

                    if (pageResults.total_count > 0)
                    {
                        var armies = multiQuery.Read<ArmyListItem>().ToList();

                        pageResults.results = armies;
                    }

                    return pageResults;
                }
            );

            return pageResults;
        }

        public async Task<Army?> GetArmy(long armyGuid)
        {
            const string SELECT_SQL = @"
                SELECT
                    a.army_guid,
                    EXTRACT(EPOCH FROM established_at)::bigint AS established_at,
                    is_recruiting,
                    motd,
                    a.name,
                    tag,
                    playstyle,
                    description,
                    personality,
                    website,
                    login_message,
                    region,
                    (SELECT COUNT(*) FROM webapi.""ArmyMembers"" am WHERE am.army_guid = a.army_guid) AS member_count
                FROM webapi.""Armies"" a
                WHERE a.army_guid = @armyGuid;

                SELECT
                    ar.name AS rank_name,
                    c.name,
                    true as is_online
                FROM webapi.""ArmyMembers"" am
                LEFT JOIN webapi.""Characters"" c USING (character_guid)
                LEFT JOIN webapi.""ArmyRanks"" ar USING (army_rank_id)
                WHERE am.army_guid = @armyGuid AND ar.is_officer = true";

            var army = await DBCall(
                async conn =>
                {
                    await using var multiQuery = await conn.QueryMultipleAsync(SELECT_SQL, new { armyGuid });
                    var army = multiQuery.Read<Army>().FirstOrDefault();
                    var officers = multiQuery.Read<ArmyOfficer>().ToList();

                    if (army != null)
                    {
                        army.officers = officers;
                    }

                    return army;
                }
            );

            return army;
        }

        public async Task<long> CreateArmy(long commanderGuid, string name, string? website, string description,
            bool isRecruiting, string playstyle, string region, string personality)
        {
            var result = await DBCall(async conn =>
            {
                var p = new DynamicParameters();
                p.Add("@commander_guid", commanderGuid);
                p.Add("@name", name);
                p.Add("@website", website);
                p.Add("@description", description);
                p.Add("@is_recruiting", isRecruiting);
                p.Add("@playstyle", playstyle);
                p.Add("@region", region);
                p.Add("@personality", personality);

                p.Add("@error_text", dbType: DbType.String, direction: ParameterDirection.Output);
                p.Add("@army_guid", dbType: DbType.Int64, direction: ParameterDirection.Output);

                var r = await conn.ExecuteAsync("webapi.\"CreateArmy\"", p, commandType: CommandType.StoredProcedure);

                return (p.Get<long>("@army_guid"), p.Get<string>("@error_text"));
            });

            if (result.Item1 == -1)
            {
                throw new TmwException(result.Item2, result.Item2);
            }

            return result.Item1;
        }

        public async Task<bool> UpdateArmy(long armyGuid, bool isRecruiting, string playstyle, string personality,
            string region, string? motd, string? website, string? description, string? loginMessage)
        {
            var parameters = new DynamicParameters();
            parameters.Add("armyGuid", armyGuid);

            var setClauses = new List<string>
            {
                "is_recruiting = @isRecruiting",
                "playstyle = @playstyle",
                "personality = @personality",
                "region = @region"
            };
            parameters.Add("isRecruiting", isRecruiting);
            parameters.Add("playstyle", playstyle);
            parameters.Add("personality", personality);
            parameters.Add("region", region);

            if (motd != null)
            {
                setClauses.Add("motd = @motd");
                parameters.Add("motd", motd);
            }

            if (website != null)
            {
                setClauses.Add("website = @website");
                parameters.Add("website", website);
            }

            if (description != null)
            {
                setClauses.Add("description = @description");
                parameters.Add("description", description);
            }

            if (loginMessage != null)
            {
                setClauses.Add("login_message = @loginMessage");
                parameters.Add("loginMessage", loginMessage);
            }

            var updateSql =
                $"UPDATE webapi.\"Armies\" SET {string.Join(", ", setClauses)} WHERE army_guid = @armyGuid;";

            var result = await DBCall(conn => conn.ExecuteAsync(updateSql, parameters));

            return result > 0;
        }

        public async Task<bool> EstablishArmyTag(long armyGuid, string tag)
        {
            string UPDATE_SQL = @"
                UPDATE webapi.""Armies""
                SET tag = @tag
                WHERE army_guid = @armyGuid;";

            var result = await DBCall(async conn => await conn.ExecuteAsync(UPDATE_SQL, new { armyGuid, tag }));

            return result > 0;
        }

        public async Task<bool> DisbandArmy(long armyGuid)
        {
            const string DELETE_SQL = @"
                DELETE FROM webapi.""Armies""
                WHERE army_guid = @armyGuid;";

            var result = await DBCall(async conn => await conn.ExecuteAsync(DELETE_SQL, new { armyGuid }));

            return result > 0;
        }

        public async Task<bool> StepDownAsCommander(long armyGuid, long commanderGuid, string newCommanderName)
        {
            var result = await DBCall(async conn =>
            {
                var p = new DynamicParameters();
                p.Add("@army_guid", armyGuid);
                p.Add("@commander_guid", commanderGuid);
                p.Add("@new_commander_name", newCommanderName);

                p.Add("@result", dbType: DbType.Boolean, direction: ParameterDirection.Output);

                var r = await conn.ExecuteAsync(
                    "webapi.\"StepDownAsCommander\"",
                    p,
                    commandType: CommandType.StoredProcedure
                );

                return p.Get<bool>("@result");
            });

            return result;
        }

        public async Task<bool> LeaveArmy(long armyGuid, long characterGuid)
        {
            const string DELETE_SQL = @"
                DELETE FROM webapi.""ArmyMembers"" am
                WHERE character_guid = @characterGuid AND army_guid = @armyGuid;";

            var result = await DBCall(async conn => await conn.ExecuteAsync(DELETE_SQL, new { characterGuid, armyGuid }));

            return result > 0;
        }

        public async Task<List<ArmyRank>> GetArmyRanks(long armyGuid)
        {
            const string SELECT_SQL = @"
                SELECT 
                    army_rank_id as id,
                    army_guid,
                    name,
                    is_commander,
                    can_invite,
                    can_kick,
                    created_at,
                    updated_at,
                    can_edit,
                    can_promote,
                    position,
                    is_officer,
                    can_mass_email,
                    is_default
                FROM webapi.""ArmyRanks""
                WHERE army_guid = @armyGuid
                ORDER BY position;";

            var results = await DBCall(async conn => await conn.QueryAsync<ArmyRank>(SELECT_SQL, new { armyGuid }));

            return results.ToList();
        }

        public async Task<bool> AddArmyRank(long armyGuid, bool canPromote, bool canEdit,
            bool isOfficer, string name, bool canInvite, bool canKick, int position)
        {
            const string INSERT_SQL = @"
                INSERT INTO webapi.""ArmyRanks"" (
                      army_guid, can_promote, can_edit, 
                      is_officer, name, can_invite, can_kick, position, created_at)
                VALUES (@armyGuid, @canPromote, @canEdit, 
                        @isOfficer, @name, @canInvite, @canKick, @position, NOW());";

            var result = await DBCall(async conn => await conn.ExecuteAsync(
                INSERT_SQL,
                new { armyGuid, canPromote, canEdit, isOfficer, name, canInvite, canKick, position }
            ));

            return result > 0;
        }

        public async Task<bool> ReassignMembersAndRemoveArmyRank(long armyGuid, long rankId, long defaultRankId)
        {
            const string UPDATE_AND_DELETE_SQL = @"
                UPDATE webapi.""ArmyMembers""
                SET army_rank_id = @defaultRankId
                WHERE army_guid = @armyGuid AND army_rank_id = @rankId;

                DELETE FROM webapi.""ArmyRanks""
                WHERE army_guid = @armyGuid AND army_rank_id = @rankId;";

            var result = await DBCall(async conn =>
            {
                using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                var r = await conn.ExecuteAsync(UPDATE_AND_DELETE_SQL, new { armyGuid, rankId, defaultRankId });

                tx.Complete();

                return r;
            });

            return result > 0;
        }

        public async Task<bool> UpdateArmyRank(long armyGuid, int initiatorRankPosition, long rankId, bool canPromote, bool canEdit,
            bool isOfficer, string? name, bool canInvite, bool canKick)
        {
            const string UPDATE_SQL = @"
                UPDATE webapi.""ArmyRanks""
                SET 
                    name = COALESCE(@name, name),
                    can_edit = @canEdit,
                    can_invite = @canInvite,
                    can_kick = @canKick,
                    is_officer = @isOfficer,
                    can_promote = @canPromote,
                    updated_at = CURRENT_TIMESTAMP
                WHERE army_guid = @armyGuid AND army_rank_id = @rankId AND position > @initiatorRankPosition;";

            var result = await DBCall(async conn => await conn.ExecuteAsync(
                UPDATE_SQL,
                new { armyGuid, initiatorRankPosition, rankId, name, canEdit, canInvite, canKick, isOfficer, canPromote }
            ));

            return result > 0;
        }

        public async Task<bool> UpdateArmyRanksOrder(long armyGuid, int[] rankIds)
        {
            var p = new DynamicParameters();
            p.Add("army_guid", armyGuid);
            p.Add("rank_ids", rankIds);
            p.Add("result", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            var result = await DBCall(async conn =>
                {
                    await conn.ExecuteAsync("webapi.\"UpdateArmyRanksOrder\"", p,
                        commandType: CommandType.StoredProcedure);

                    return p.Get<bool>("result");
                }
            );

            return result;
        }

        public async Task<PageResults<ArmyMember>> GetArmyMembers(long armyGuid, int page, int perPage)
        {
            const string SELECT_SQL = @"
                SELECT COUNT(*) AS total_count 
                FROM webapi.""ArmyMembers"" am
                WHERE am.army_guid = @armyGuid;

                SELECT 
                    am.character_guid,
                    am.army_rank_id,
                    ar.position AS rank_position,
                    ar.name AS rank_name,
                    EXTRACT(EPOCH FROM last_seen_at)::bigint AS last_seen_at,
                    am.public_note,
                    c.name,
                    b.battleframe_sdb_id AS current_frame_sdb_id,
                    b.level AS current_level,
                    448 as last_zone_id,
                    true as is_online
                FROM webapi.""ArmyMembers"" am
                INNER JOIN webapi.""Characters"" c USING (character_guid)
                INNER JOIN webapi.""ArmyRanks"" ar USING (army_rank_id)
                INNER JOIN webapi.""Battleframes"" b ON b.id = c.current_battleframe_guid
                WHERE am.army_guid = @armyGuid
                ORDER BY last_seen_at DESC
                LIMIT @perPage OFFSET @offset";

            var p = new DynamicParameters();
            p.Add("armyGuid", armyGuid);
            p.Add("perPage", perPage);
            p.Add("offset", (page - 1) * perPage);


            var pageResults = await DBCall(
                async conn =>
                {
                    await using var multiQuery = await conn.QueryMultipleAsync(SELECT_SQL, p);

                    var pageResults = multiQuery.Read<PageResults<ArmyMember>>().Single();
                    pageResults.page = page.ToString();

                    if (pageResults.total_count > 0)
                    {
                        var armyMembers = multiQuery.Read<ArmyMember>().ToList();

                        pageResults.results = armyMembers;
                    }

                    return pageResults;
                }
            );

            return pageResults;
        }

        public async Task<ArmyRank> GetArmyRankForMember(long armyGuid, long characterGuid)
        {
            const string SELECT_SQL = @"
                SELECT 
                    ar.army_rank_id as id,
                    ar.army_guid,
                    ar.name,
                    is_commander,
                    can_invite,
                    can_kick,
                    ar.created_at,
                    updated_at,
                    can_edit,
                    can_promote,
                    position,
                    is_officer,
                    can_mass_email,
                    is_default
                FROM webapi.""ArmyRanks"" ar
                INNER JOIN webapi.""ArmyMembers"" am ON am.army_rank_id = ar.army_rank_id
                INNER JOIN webapi.""Characters"" c on c.character_guid = am.character_guid
                WHERE am.character_guid = @characterGuid;";

            return await DBCall(async conn => await conn.QuerySingleAsync<ArmyRank>(SELECT_SQL, new { armyGuid, characterGuid }));
        }

        public async Task<bool> UpdateArmyRankForMembers(long armyGuid, long initiatorGuid, long rankId, long[] characterGuids)
        {
            var initiatorRankPosition = await GetArmyRankPosition(armyGuid, initiatorGuid);

            const string UPDATE_SQL = @"
                UPDATE webapi.""ArmyMembers"" am
                SET army_rank_id = @rankId
                FROM webapi.""ArmyRanks"" ar
                WHERE ar.army_rank_id = @rankId AND ar.position > 1 AND ar.position >= @initiatorRankPosition
                AND am.army_guid = @armyGuid AND character_guid = ANY(@characterGuids);";

            var result = await DBCall(
                async conn => await conn.ExecuteAsync(UPDATE_SQL, new { armyGuid, initiatorRankPosition, rankId, characterGuids })
            );

            return result > 0;
        }

        public async Task<bool> UpdateArmyMemberPublicNote(long armyGuid, long characterGuid, string publicNote)
        {
            const string UPDATE_SQL = @"
                UPDATE webapi.""ArmyMembers""
                SET public_note = @publicNote
                WHERE character_guid = @characterGuid
                AND army_guid = @armyGuid;";

            var result = await DBCall(async conn => await conn.ExecuteAsync(UPDATE_SQL, new { characterGuid, armyGuid, publicNote }));

            return result > 0;
        }

        public async Task<int> KickArmyMembers(long armyGuid, long initiatorGuid, long[] characterGuids)
        {
            var initiatorRankPosition = await GetArmyRankPosition(armyGuid, initiatorGuid);

            const string DELETE_SQL = @"
                DELETE FROM webapi.""ArmyMembers"" am
                    USING webapi.""ArmyRanks"" ar
                WHERE 
                    am.army_rank_id = ar.army_rank_id
                    AND am.army_guid = @armyGuid 
                    AND am.character_guid = ANY(@characterGuids)
                    AND ar.position > @initiatorRankPosition;";

            var result = await DBCall(async conn => await conn.ExecuteAsync(DELETE_SQL, new { armyGuid, initiatorRankPosition, characterGuids }));

            return result;
        }

        public async Task<object> GetArmyApplications(long armyGuid)
        {
            const string SELECT_SQL = @"
                SELECT 
                    army_guid, 
                    message, 
                    id, 
                    name,
                    'apply' as direction
                FROM webapi.""ArmyApplications"" aa
                INNER JOIN webapi.""Characters"" c USING (character_guid)
                WHERE army_guid = @armyGuid AND invite = false";

            var results = await DBCall(async conn => await conn.QueryAsync<dynamic>(SELECT_SQL, new { armyGuid }));

            return results;
        }

         public async Task<bool> ApproveArmyApplicationsOrInvites(long armyGuid, long[] applicationIds)
         {
             var result = await DBCall(async conn =>
             {
                 var p = new DynamicParameters();
                 p.Add("@army_guid", armyGuid);
                 p.Add("@application_ids", applicationIds);

                 p.Add("@result", dbType: DbType.Boolean, direction: ParameterDirection.Output);

                 var r = await conn.ExecuteAsync(
                     "webapi.\"ApproveArmyApplicationsOrInvites\"",
                     p,
                     commandType: CommandType.StoredProcedure
                 );

                 return p.Get<bool>("@result");
             });

             return result;
         }

        public async Task<bool> RejectArmyApplicationsOrInvites(long armyGuid, long[] ids)
        {
            const string DELETE_SQL = @"
                DELETE FROM webapi.""ArmyApplications""
                WHERE army_guid = @armyGuid AND id = ANY(@ids);";

            var result = await DBCall(async conn => await conn.ExecuteAsync(DELETE_SQL, new { armyGuid, ids }));

            return result > 0;
        }

        public async Task<bool> InviteToArmy(long armyGuid, string characterName, string message)
        {
            var result = await DBCall(async conn =>
            {
                var p = new DynamicParameters();
                p.Add("@army_guid", armyGuid);
                p.Add("@character_name", characterName);
                p.Add("@message", message);

                p.Add("@result", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("@error_text", dbType: DbType.String, direction: ParameterDirection.Output);

                var r = await conn.ExecuteAsync("webapi.\"InviteToArmy\"", p, commandType: CommandType.StoredProcedure);

                return (p.Get<bool>("@result"), p.Get<string>("@error_text"));
            });

            if (!result.Item1)
            {
                throw new TmwException(result.Item2, result.Item2);
            }

            return result.Item1;
        }

        public async Task<bool> ApplyToArmy(long armyGuid, long characterGuid, string message)
        {
            const string INSERT_SQL = @"
                INSERT INTO webapi.""ArmyApplications"" (army_guid, character_guid, message)
                VALUES (@armyGuid, @characterGuid, @message);";

            var result = await DBCall(async conn => await conn.ExecuteAsync(INSERT_SQL, new { armyGuid, characterGuid, message }));

            return result > 0;
        }

        public async Task<int> GetArmyMemberCount(long armyGuid)
        {
            const string SELECT_SQL = @"
                SELECT COUNT(*) FROM webapi.""ArmyMembers""
                WHERE army_guid = @armyGuid;";

            return await DBCall(async conn => await conn.QuerySingleAsync<int>(SELECT_SQL, new { armyGuid }));
        }

        public async Task<bool> ArmyMemberHasPermission(long armyGuid, long characterGuid, ArmyPermission permission)
        {
            var selectSql = $@"
                SELECT ar.{permission}
                FROM webapi.""ArmyMembers"" am
                INNER JOIN webapi.""ArmyRanks"" ar USING (army_rank_id)
                WHERE am.army_guid = @armyGuid AND am.character_guid = @characterGuid;";

            return await DBCall(async conn => await conn.QuerySingleAsync<bool>(selectSql, new { armyGuid, characterGuid }));
        }

        public async Task<bool> HasInviteFromArmy(long armyGuid, long characterGuid)
        {
            const string SELECT_SQL = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM webapi.""ArmyApplications""
                    WHERE army_guid = @armyGuid AND character_guid = @characterGuid AND invite = true
                );";

            return await DBCall(async conn => await conn.QuerySingleAsync<bool>(SELECT_SQL, new { armyGuid, characterGuid }));
        }

        public async Task<bool> HasInviteFromArmyOrApplied(long armyGuid, long characterGuid)
        {
            const string SELECT_SQL = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM webapi.""ArmyApplications""
                    WHERE army_guid = @armyGuid AND character_guid = @characterGuid
                );";

            return await DBCall(async conn => await conn.QuerySingleAsync<bool>(SELECT_SQL, new { armyGuid, characterGuid }));
        }

        public async Task<int> GetArmyRankPosition(long armyGuid, long characterGuid)
        {
            const string SELECT_SQL = @"
                SELECT ar.position
                FROM webapi.""ArmyMembers"" am
                INNER JOIN webapi.""ArmyRanks"" ar ON am.army_rank_id = ar.army_rank_id
                WHERE am.army_guid = @armyGuid AND am.character_guid = @characterGuid;";

            var rankPosition = await DBCall(async conn => await conn.QuerySingleAsync<int>(SELECT_SQL, new { armyGuid, characterGuid }));

            return rankPosition;
        }

        public async Task<long> GetDefaultArmyRankId(long armyGuid)
        {
            const string SELECT_SQL = @"
                SELECT army_rank_id
                FROM webapi.""ArmyRanks""
                WHERE army_guid = @armyGuid AND is_default = true;";

            return await DBCall(async conn => await conn.QuerySingleAsync<long>(SELECT_SQL, new { armyGuid }));
        }
    }
}
