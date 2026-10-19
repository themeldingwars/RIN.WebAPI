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
        public async Task<long?> GetBattleframeId(long characterId, int battleframeSdbId)
        {
            const string SELECT_SQL = @"SELECT id
                                        FROM webapi.""Battleframes""
                                        WHERE character_guid = @characterId
                                          AND battleframe_sdb_id = @battleframeSdbId
                                        ORDER BY id
                                        LIMIT 1";

            return await DBCall(conn => conn.QueryFirstOrDefaultAsync<long?>(SELECT_SQL, new { characterId, battleframeSdbId }));
        }

        public async ValueTask<long?> CreateBattleframeLoadout(long characterId, int battleframeSdId, PlayerBattleframeVisuals visuals)
        {
            const string INSERT_SQL = @"INSERT INTO webapi.""Battleframes""(
		                        character_guid, battleframe_sdb_id, visuals, hidden, level, xp, elite_level, elite_xp, id)
		                        VALUES (@characterId, @battleframeSdId, @visuals, false, 1, 0, 0, 0, webapi.create_entity_guid(253))
                            RETURNING id";

            byte[] visualsData = Utils.MiscUtils.ToProtoBuffByteArray(visuals);
            var result = await DBCall(async conn => conn.Query<long>(INSERT_SQL, new { characterId, battleframeSdId, visuals = visualsData }),
                exception =>
                {
                    Logger.LogError("Error creating a battleframe loadout ({battleframeSdId}) for {characterId} due to: {exception}", characterId, battleframeSdId, exception);
                    throw exception;
                });

            return result.Single();
        }

        public async Task<long?> EnsureBattleframeRecord(long characterId, int battleframeSdbId, PlayerBattleframeVisuals? visuals = null)
        {
            var existingId = await GetBattleframeId(characterId, battleframeSdbId);
            if (existingId.HasValue && existingId.Value > 0)
            {
                return existingId.Value;
            }

            return await CreateBattleframeLoadout(characterId, battleframeSdbId, visuals ?? PlayerBattleframeVisuals.CreateDefault());
        }

        public async Task<bool> EnsureBattleframeRecords(long characterId, IEnumerable<int> battleframeSdbIds)
        {
            bool changed = false;

            foreach (var battleframeSdbId in battleframeSdbIds.Where(id => id > 0).Distinct())
            {
                var existingId = await GetBattleframeId(characterId, battleframeSdbId);
                if (existingId.HasValue && existingId.Value > 0)
                {
                    continue;
                }

                var createdId = await CreateBattleframeLoadout(characterId, battleframeSdbId, PlayerBattleframeVisuals.CreateDefault());
                if (createdId.HasValue && createdId.Value > 0)
                {
                    changed = true;
                }
            }

            return changed;
        }

        public async Task<bool> SetCharacterCurrentBattleframeBySdbId(long charId, int battleframeSdbId)
        {
            var battleframeId = await EnsureBattleframeRecord(charId, battleframeSdbId);
            if (!battleframeId.HasValue || battleframeId.Value <= 0)
            {
                return false;
            }

            return await SetCharacterCurrentBattleframe(charId, battleframeId.Value);
        }

        public async Task<bool> UpdateBattleframeVisuals(long battleframeId, PlayerBattleframeVisuals visuals)
        {
            return await DBCall(async conn =>
            {
                await using var tx = await conn.BeginTransactionAsync();

                const string UPDATE_SQL = @"UPDATE webapi.""Battleframes""
		                        SET visuals = @visualsBlob
		                        WHERE id = @battleframeId";

                var visualsBlob = Utils.MiscUtils.ToProtoBuffByteArray(visuals);
                var result = await conn.ExecuteAsync(UPDATE_SQL, new { battleframeId, visualsBlob }, tx);

                var characterGuid = await conn.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT character_guid
                      FROM webapi.""Battleframes""
                      WHERE id = @battleframeId",
                    new { battleframeId }, tx);

                var currentBattleframeId = characterGuid.HasValue
                    ? await conn.QueryFirstOrDefaultAsync<long?>(
                        @"SELECT current_battleframe_guid
                          FROM webapi.""Characters""
                          WHERE character_guid = @characterGuid",
                        new { characterGuid }, tx)
                    : null;

                if (characterGuid.HasValue && currentBattleframeId.HasValue && currentBattleframeId.Value == battleframeId)
                {
                    await conn.ExecuteAsync(
                        @"SELECT pg_notify('events', 'CharacterVisualsUpdated->' || json_build_object('CharacterGuid', @characterGuid)::text);",
                        new { characterGuid }, tx);
                }

                await tx.CommitAsync();
                return result > 0;
            });
        }

        public async Task<PlayerBattleframeVisuals?> GetCurrentBattleframeVisuals(long characterId)
        {
            const string SELECT_SQL = @"SELECT bf.visuals
                                        FROM webapi.""Characters"" c
                                        LEFT JOIN webapi.""Battleframes"" bf ON bf.id = c.current_battleframe_guid
                                        WHERE c.character_guid = @characterId";

            var visualsBlob = await DBCall(conn => conn.QueryFirstOrDefaultAsync<byte[]>(SELECT_SQL, new { characterId }));

            if (visualsBlob == null || visualsBlob.Length == 0)
            {
                return null;
            }

            try
            {
                return Utils.MiscUtils.FromProtoBuffByteArray<PlayerBattleframeVisuals>(visualsBlob.AsSpan());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to deserialize battleframe visuals for character {characterId}", characterId);
                return null;
            }
        }

        public async Task<Dictionary<int, (long BattleframeGuid, PlayerBattleframeVisuals Visuals)>> GetBattleframeVisualsByChassis(long characterId)
        {
            const string SELECT_SQL = @"SELECT id,
                                               battleframe_sdb_id,
                                               visuals,
                                               (id = COALESCE((SELECT current_battleframe_guid
                                                               FROM webapi.""Characters""
                                                               WHERE character_guid = @characterId), 0)) AS is_current
                                        FROM webapi.""Battleframes""
                                        WHERE character_guid = @characterId";

            var rows = await DBCall(conn => conn.QueryAsync<(long id, int battleframe_sdb_id, byte[] visuals, bool is_current)>(
                SELECT_SQL,
                new { characterId })) ?? Enumerable.Empty<(long id, int battleframe_sdb_id, byte[] visuals, bool is_current)>();

            var result = new Dictionary<int, (long BattleframeGuid, PlayerBattleframeVisuals Visuals)>();
            var selectedCurrentByChassis = new Dictionary<int, bool>();
            foreach (var row in rows)
            {
                PlayerBattleframeVisuals visuals;
                if (row.visuals != null && row.visuals.Length > 0)
                {
                    try
                    {
                        visuals = Utils.MiscUtils.FromProtoBuffByteArray<PlayerBattleframeVisuals>(row.visuals.AsSpan())
                                  ?? PlayerBattleframeVisuals.CreateDefault();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex,
                            "GetBattleframeVisualsByChassis: failed to deserialize visuals for char={CharId}, battleframeGuid={BattleframeGuid}, chassis={ChassisSdbId}",
                            characterId, row.id, row.battleframe_sdb_id);
                        visuals = PlayerBattleframeVisuals.CreateDefault();
                    }
                }
                else
                {
                    visuals = PlayerBattleframeVisuals.CreateDefault();
                }

                if (!result.TryGetValue(row.battleframe_sdb_id, out var existing))
                {
                    result[row.battleframe_sdb_id] = (row.id, visuals);
                    selectedCurrentByChassis[row.battleframe_sdb_id] = row.is_current;
                    continue;
                }

                var existingIsCurrent = selectedCurrentByChassis.TryGetValue(row.battleframe_sdb_id, out var isCurrent)
                    && isCurrent;
                var shouldReplace = (row.is_current && !existingIsCurrent)
                    || (row.is_current == existingIsCurrent && row.id > existing.BattleframeGuid);

                if (shouldReplace)
                {
                    result[row.battleframe_sdb_id] = (row.id, visuals);
                    selectedCurrentByChassis[row.battleframe_sdb_id] = row.is_current;
                }
            }

            return result;
        }

        public async Task<(long CurrentBattleframeGuid, int CurrentBattleframeSdbId)> GetCurrentBattleframeInfo(long characterId)
        {
            const string SELECT_SQL = @"SELECT
                                            COALESCE(c.current_battleframe_guid, 0) AS CurrentBattleframeGuid,
                                            COALESCE(bf.battleframe_sdb_id, 0) AS CurrentBattleframeSdbId
                                        FROM webapi.""Characters"" c
                                        LEFT JOIN webapi.""Battleframes"" bf ON bf.id = c.current_battleframe_guid
                                        WHERE c.character_guid = @characterId";

            var result = await DBCall(conn => conn.QueryFirstOrDefaultAsync<(long CurrentBattleframeGuid, int CurrentBattleframeSdbId)>(
                SELECT_SQL,
                new { characterId }));

            return result;
        }
    }
}
