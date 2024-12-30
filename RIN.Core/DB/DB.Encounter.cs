using Dapper;
using RIN.Core.Models.ClientApi;

namespace RIN.Core.DB;

public partial class DB
{
    public async Task<Leaderboard?> GetLeaderboard(int leaderboardId, int page)
    {
        const int perPage = 15;

        const string SELECT_SQL = @"
                SELECT 
                    COUNT(*) AS total_count, 
                    l.name
                FROM webapi.""LeaderboardEntries"" le
                INNER JOIN webapi.""Leaderboards"" l ON le.leaderboard_id = l.id
                WHERE l.id = @leaderboardId
                GROUP BY l.name;

                SELECT
                    RANK() OVER (ORDER BY sorted_value) AS rank,
                    name AS character_name,
                    value AS current_value
                FROM (
                    SELECT
                        le.*, c.name,
                        CASE WHEN l.order = 0 THEN value ELSE -value END AS sorted_value
                    FROM webapi.""LeaderboardEntries"" le
                    INNER JOIN webapi.""Characters"" c USING (character_guid)
                    INNER JOIN webapi.""Leaderboards"" l ON le.leaderboard_id = l.id
                    WHERE le.leaderboard_id = @leaderboardId
                ) AS sorted
            LIMIT 15 OFFSET @offset";

        var p = new DynamicParameters();
        p.Add("leaderboardId", leaderboardId);
        p.Add("perPage", perPage);
        p.Add("offset", (page - 1) * perPage);

        var pageResults = await DBCall(
            async conn =>
            {
                await using var multiQuery = await conn.QueryMultipleAsync(SELECT_SQL, p);

                var pageResults = multiQuery.Read<Leaderboard>().SingleOrDefault();
                if (pageResults == null)
                {
                    return null;
                }
                pageResults.page = page;

                if (pageResults.total_count > 0)
                {
                    var results = multiQuery.Read<LeaderboardResult>().ToList();

                    pageResults.results = results;
                }

                return pageResults;
            }
        );

        return pageResults;
    }

    public async Task<LeaderboardResult> GetLeaderboardResult(int leaderboardId, long characterGuid)
    {
        const string SELECT_SQL = @"
            WITH LeaderboardData AS (
                SELECT
                    le.value,
                    le.character_guid,
                    le.leaderboard_id,
                    CASE WHEN l.order = 0 THEN value ELSE -value END AS sorted_value
                FROM webapi.""LeaderboardEntries"" le
                INNER JOIN webapi.""Leaderboards"" l ON le.leaderboard_id = l.id
                WHERE le.leaderboard_id = @leaderboardId
            ),
            RankedEntries AS (
                SELECT
                    CASE WHEN ld.character_guid IS NOT NULL THEN RANK() OVER (ORDER BY sorted_value) ELSE 0 END AS rank,
                    c.name,
                    COALESCE(ld.value, 0) AS current_value,
                    c.character_guid
                FROM webapi.""Characters"" c
                LEFT JOIN LeaderboardData ld USING(character_guid)
            )
            SELECT
                re.rank,
                re.name,
                COALESCE(re.current_value, 0) AS current_value
            FROM RankedEntries re
            WHERE re.character_guid = @characterGuid";

        var result = await DBCall(async conn => await conn.QuerySingleAsync<LeaderboardResult>(
            SELECT_SQL, new { leaderboardId, characterGuid }));

        return result;
    }
}
