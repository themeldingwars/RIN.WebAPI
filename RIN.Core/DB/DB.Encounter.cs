using Dapper;
using RIN.Core.Models.ClientApi;

namespace RIN.Core.DB;

public partial class DB
{
    public async Task<Leaderboard?> GetLeaderboard(int leaderboardId, int page)
    {
        const int PER_PAGE = 15;

        if (leaderboardId == int.MaxValue)
        {
            return await GetLeaderboardList(page, PER_PAGE);
        }

        const string SELECT_SQL = @"
                SELECT
                    COUNT(*) AS total_count,
                    l.name
                FROM webapi.""LeaderboardEntries"" le
                INNER JOIN webapi.""Leaderboards"" l ON le.leaderboard_id = l.id
                WHERE l.id = @leaderboardId
                GROUP BY l.name;

                -- type 1 = race time in ms, shown as m:ss.fff (h:mm:ss.fff past an hour)
                SELECT
                    RANK() OVER (ORDER BY sorted_value) AS rank,
                    name AS character_name,
                    CASE WHEN type = 1 THEN
                        to_char(value * interval '1 millisecond',
                                CASE WHEN value >= 3600000 THEN 'FMHH24:MI:SS.MS' ELSE 'FMMI:SS.MS' END)
                    ELSE value::text END AS current_value
                FROM (
                    SELECT
                        le.*, c.name, l.type,
                        CASE WHEN l.order = 0 THEN value ELSE -value END AS sorted_value
                    FROM webapi.""LeaderboardEntries"" le
                    INNER JOIN webapi.""Characters"" c USING (character_guid)
                    INNER JOIN webapi.""Leaderboards"" l ON le.leaderboard_id = l.id
                    WHERE le.leaderboard_id = @leaderboardId
                ) AS sorted
            ORDER BY rank
            LIMIT @perPage OFFSET @offset";

        var p = new DynamicParameters();
        p.Add("leaderboardId", leaderboardId);
        p.Add("perPage", PER_PAGE);
        p.Add("offset", (page - 1) * PER_PAGE);

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

    private async Task<Leaderboard> GetLeaderboardList(int page, int perPage)
    {
        const string SELECT_SQL = @"
                SELECT
                    COUNT(*) AS total_count
                FROM webapi.""Leaderboards"";

                SELECT
                    id AS rank,
                    name as character_name,
                    id as current_value
                FROM webapi.""Leaderboards""
                ORDER BY id
                LIMIT @perPage OFFSET @offset";

        var p = new DynamicParameters();
        p.Add("perPage", perPage);
        p.Add("offset", (page - 1) * perPage);

        var pageResults = await DBCall(
            async conn =>
            {
                await using var multiQuery = await conn.QueryMultipleAsync(SELECT_SQL, p);

                var pageResults = multiQuery.Read<Leaderboard>().Single();
                pageResults.name = "All Leaderboards";
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
                    l.type,
                    CASE WHEN l.order = 0 THEN value ELSE -value END AS sorted_value
                FROM webapi.""LeaderboardEntries"" le
                INNER JOIN webapi.""Leaderboards"" l ON le.leaderboard_id = l.id
                WHERE le.leaderboard_id = @leaderboardId
            ),
            RankedEntries AS (
                SELECT
                    CASE WHEN ld.character_guid IS NOT NULL THEN RANK() OVER (ORDER BY sorted_value) ELSE 0 END AS rank,
                    c.name,
                    -- type 1 = race time in ms, see GetLeaderboard
                    CASE WHEN ld.type = 1 THEN
                        to_char(ld.value * interval '1 millisecond',
                                CASE WHEN ld.value >= 3600000 THEN 'FMHH24:MI:SS.MS' ELSE 'FMMI:SS.MS' END)
                    ELSE COALESCE(ld.value, 0)::text END AS current_value,
                    c.character_guid
                FROM webapi.""Characters"" c
                LEFT JOIN LeaderboardData ld USING(character_guid)
            )
            SELECT
                re.rank,
                re.name,
                re.current_value
            FROM RankedEntries re
            WHERE re.character_guid = @characterGuid";

        var result = await DBCall(async conn => await conn.QuerySingleAsync<LeaderboardResult>(
            SELECT_SQL, new { leaderboardId, characterGuid }));

        return result;
    }
}
