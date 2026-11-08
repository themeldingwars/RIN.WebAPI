using Dapper;

namespace RIN.Core.DB;

public partial class DB
{
    /// <summary>
    /// Where an online character is, for what the web API shows of it. A character entering a zone is also seen then.
    /// </summary>
    public async Task<bool> SetCharacterOnline(long characterGuid, int zoneId)
    {
        const string UPDATE_SQL = @"
                UPDATE webapi.""Characters""
                SET current_zone_id = @zoneId,
                    last_seen_at = now()
                WHERE character_guid = @characterGuid";

        var affected = await DBCall(conn => conn.ExecuteAsync(UPDATE_SQL, new { characterGuid, zoneId }));
        return affected > 0;
    }

    public async Task<bool> SetCharacterOffline(long characterGuid)
    {
        const string UPDATE_SQL = @"
                UPDATE webapi.""Characters""
                SET current_zone_id = NULL,
                    last_seen_at = now()
                WHERE character_guid = @characterGuid";

        var affected = await DBCall(conn => conn.ExecuteAsync(UPDATE_SQL, new { characterGuid }));
        return affected > 0;
    }

    /// <summary>
    /// Nobody is online before a game server reported it. Called when the internal API starts, its presence starts
    /// empty, so characters online when it stopped don't stay online.
    /// </summary>
    public async Task<int> ClearCharacterPresence()
    {
        const string UPDATE_SQL = @"
                UPDATE webapi.""Characters""
                SET current_zone_id = NULL
                WHERE current_zone_id IS NOT NULL";

        return await DBCall(conn => conn.ExecuteAsync(UPDATE_SQL));
    }
}
