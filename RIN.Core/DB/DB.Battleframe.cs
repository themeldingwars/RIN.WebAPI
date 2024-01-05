using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIN.Core.DB
{
    public partial class DB
    {
        public async ValueTask<long?> CreateBattleframeLoadout(long characterId, int battleframeSdId, PlayerBattleframeVisuals visuals)
        {
            const string INSERT_SQL = @"INSERT INTO webapi.""Battleframes""(
	                        character_guid, battleframe_sdb_id, visuals, hidden, level, xp, id)
	                        VALUES (@characterId, @battleframeSdId, @visuals, false, 1, 0, webapi.create_entity_guid(253))
                            RETURNING id;";

            byte[] visualsData = Utils.MiscUtils.ToProtoBuffByteArray(visuals);
            var result = await DBCall(async conn => conn.Query<long>(INSERT_SQL, new { characterId, battleframeSdId, visuals = visualsData }),
                exception =>
                {
                    Logger.LogError("Error creating a battleframe loadout ({battleframeSdId}) for {characterId} due to: {exception}", characterId, battleframeSdId, exception);
                    throw exception;
                });

            return result.Single();
        }

        public async Task<bool> UpdateBattleframeVisuals(long battleframeId, PlayerBattleframeVisuals visuals)
        {
            const string UPDATE_SQL = @"UPDATE webapi.""Battleframes""
	                            SET visuals = @visualsBlob
	                            WHERE id = @battleframeId;";

            var visualsBlob = Utils.MiscUtils.ToProtoBuffByteArray(visuals);
            var result = await DBCall(conn => conn.ExecuteAsync(UPDATE_SQL, new { battleframeId, visualsBlob }));

            return result > 0;
        }

    }
}
