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

        /// <summary>
        /// Creates the battleframe row for a chassis, or returns the existing one. Template seeding
        /// already creates a row per chassis, so asking for a frame the character has must not fail.
        /// </summary>
        public async ValueTask<long?> CreateBattleframeLoadout(long characterId, int battleframeSdId, PlayerBattleframeVisuals visuals)
        {
            const string INSERT_SQL = @"INSERT INTO webapi.""Battleframes""(
		                        character_guid, battleframe_sdb_id, visuals, hidden, level, current_xp, lifetime_xp, elite_level, current_elite_xp, lifetime_elite_xp, id)
		                        VALUES (@characterId, @battleframeSdId, @visuals, false, 1, 0, 0, 0, 0, 0, webapi.create_entity_guid(253))
                            ON CONFLICT (character_guid, battleframe_sdb_id) DO NOTHING
                            RETURNING id";

            byte[] visualsData = Utils.MiscUtils.ToProtoBuffByteArray(visuals);
            var inserted = await DBCall(conn => conn.QueryAsync<long>(INSERT_SQL, new { characterId, battleframeSdId, visuals = visualsData }),
                exception =>
                {
                    Logger.LogError("Error creating a battleframe loadout ({battleframeSdId}) for {characterId} due to: {exception}", characterId, battleframeSdId, exception);
                    throw exception;
                });

            if (inserted.Any())
            {
                return inserted.First();
            }

            return await GetBattleframeId(characterId, battleframeSdId);
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

        public async Task<IEnumerable<BattleframeProgressionRow>> GetBattleframeProgression(long characterGuid)
        {
            const string SQL = @"
                SELECT id, battleframe_sdb_id, level, current_xp, lifetime_xp,
                       elite_level, current_elite_xp, lifetime_elite_xp, elite_points, bandwidth
                FROM webapi.""Battleframes""
                WHERE character_guid = @characterGuid
                ORDER BY battleframe_sdb_id;

                SELECT p.battleframe_guid, p.upgrade_id, p.count
                FROM webapi.""BattleframePreviousUpgrades"" p
                INNER JOIN webapi.""Battleframes"" b ON p.battleframe_guid = b.id
                WHERE b.character_guid = @characterGuid
                ORDER BY p.battleframe_guid, p.upgrade_id;

                SELECT a.battleframe_guid, a.upgrade_index, a.upgrade_id, a.category, a.stat_value,
                       a.count, a.item_sdb_id, a.localization_id
                FROM webapi.""BattleframeAvailableUpgrades"" a
                INNER JOIN webapi.""Battleframes"" b ON a.battleframe_guid = b.id
                WHERE b.character_guid = @characterGuid
                ORDER BY a.battleframe_guid, a.upgrade_index;";

            return await DBCall(async conn =>
            {
                using var multi = await conn.QueryMultipleAsync(SQL, new { characterGuid });
                var frames = (await multi.ReadAsync<BattleframeProgressionRow>()).ToList();
                var previousUpgrades = (await multi.ReadAsync<BattleframePreviousUpgradeRow>()).ToList();
                var availableUpgrades = (await multi.ReadAsync<BattleframeAvailableUpgradeRow>()).ToList();

                var previousByFrame = previousUpgrades.GroupBy(x => x.battleframe_guid).ToDictionary(g => g.Key, g => g.ToList());
                var availableByFrame = availableUpgrades.GroupBy(x => x.battleframe_guid).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var frame in frames)
                {
                    if (previousByFrame.TryGetValue(frame.id, out var previousList))
                        frame.previous_upgrades = previousList;

                    if (availableByFrame.TryGetValue(frame.id, out var availableList))
                        frame.available_upgrades = availableList;
                }

                return frames;
            });
        }

        public async Task<bool> UpdateBattleframeXp(long characterGuid, int battleframeSdbId, int level, long currentXp, long lifetimeXp)
        {
            const string UPDATE_SQL = @"UPDATE webapi.""Battleframes""
		                        SET level       = @level,
		                            current_xp  = @currentXp,
		                            lifetime_xp = @lifetimeXp
		                        WHERE character_guid     = @characterGuid
		                          AND battleframe_sdb_id = @battleframeSdbId";

            var affected = await DBCall(conn => conn.ExecuteAsync(UPDATE_SQL, new { characterGuid, battleframeSdbId, level, currentXp, lifetimeXp }));
            return affected > 0;
        }

        public async Task<bool> UpdateBattleframeEliteXp(long characterGuid, int battleframeSdbId, int eliteLevel, long currentEliteXp, long lifetimeEliteXp, int elitePoints)
        {
            const string UPDATE_SQL = @"UPDATE webapi.""Battleframes""
		                        SET elite_level       = @eliteLevel,
		                            current_elite_xp  = @currentEliteXp,
		                            lifetime_elite_xp = @lifetimeEliteXp,
		                            elite_points      = @elitePoints
		                        WHERE character_guid     = @characterGuid
		                          AND battleframe_sdb_id = @battleframeSdbId";

            var affected = await DBCall(conn => conn.ExecuteAsync(UPDATE_SQL, new { characterGuid, battleframeSdbId, eliteLevel, currentEliteXp, lifetimeEliteXp, elitePoints }));
            return affected > 0;
        }

        /// <summary>
        /// Sets a battleframe's perk point budget, shared by every loadout of that chassis. PIN computes
        /// the number; RIN stores it and validates nothing against it.
        /// </summary>
        public async Task<bool> UpdateBattleframeBandwidth(long characterGuid, int battleframeSdbId, int bandwidth)
        {
            const string UPDATE_SQL = @"UPDATE webapi.""Battleframes""
		                        SET bandwidth = @bandwidth
		                        WHERE character_guid     = @characterGuid
		                          AND battleframe_sdb_id = @battleframeSdbId";

            var affected = await DBCall(conn => conn.ExecuteAsync(UPDATE_SQL, new { characterGuid, battleframeSdbId, bandwidth }));
            return affected > 0;
        }

        public async Task<bool> SaveBattleframeUpgrade(long characterGuid, int battleframeSdbId, int upgradeId, int count)
        {
            const string UPSERT_SQL = @"INSERT INTO webapi.""BattleframePreviousUpgrades"" (battleframe_guid, upgrade_id, count)
                                        SELECT b.id, @upgradeId, @count
                                        FROM webapi.""Battleframes"" b
                                        WHERE b.character_guid = @characterGuid
                                          AND b.battleframe_sdb_id = @battleframeSdbId
                                        ON CONFLICT (battleframe_guid, upgrade_id)
                                        DO UPDATE SET count = EXCLUDED.count";

            var affected = await DBCall(conn => conn.ExecuteAsync(UPSERT_SQL, new { characterGuid, battleframeSdbId, upgradeId, count }));
            return affected > 0;
        }

        public async Task<bool> ClearBattleframeUpgrades(long characterGuid, int battleframeSdbId)
        {
            const string DELETE_SQL = @"DELETE FROM webapi.""BattleframePreviousUpgrades""
                                        WHERE battleframe_guid IN (SELECT id
                                                                   FROM webapi.""Battleframes""
                                                                   WHERE character_guid = @characterGuid
                                                                     AND battleframe_sdb_id = @battleframeSdbId)";

            var affected = await DBCall(conn => conn.ExecuteAsync(DELETE_SQL, new { characterGuid, battleframeSdbId }));
            return affected >= 0;
        }

        public async Task<bool> SaveBattleframeAvailableUpgrades(long characterGuid, int battleframeSdbId, List<BattleframeAvailableUpgradeRow> upgrades)
        {
            const string DELETE_SQL = @"DELETE FROM webapi.""BattleframeAvailableUpgrades""
                                        WHERE battleframe_guid IN (SELECT id
                                                                   FROM webapi.""Battleframes""
                                                                   WHERE character_guid = @characterGuid
                                                                     AND battleframe_sdb_id = @battleframeSdbId)";

            const string INSERT_SQL = @"INSERT INTO webapi.""BattleframeAvailableUpgrades""
		                        (battleframe_guid, upgrade_index, upgrade_id, category, stat_value, count, item_sdb_id, localization_id)
		                        SELECT b.id, @upgradeIndex, @upgradeId, @category, @statValue, @count, @itemSdbId, @localizationId
		                        FROM webapi.""Battleframes"" b
		                        WHERE b.character_guid = @characterGuid
		                          AND b.battleframe_sdb_id = @battleframeSdbId";

            return await DBCall(async conn =>
            {
                await using var tx = await conn.BeginTransactionAsync();

                await conn.ExecuteAsync(DELETE_SQL, new { characterGuid, battleframeSdbId }, tx);

                foreach (var upgrade in upgrades ?? new List<BattleframeAvailableUpgradeRow>())
                {
                    await conn.ExecuteAsync(INSERT_SQL, new
                    {
                        characterGuid,
                        battleframeSdbId,
                        upgradeIndex    = upgrade.upgrade_index,
                        upgradeId       = upgrade.upgrade_id,
                        category        = upgrade.category,
                        statValue       = upgrade.stat_value,
                        count           = upgrade.count,
                        itemSdbId       = upgrade.item_sdb_id,
                        localizationId  = upgrade.localization_id
                    }, tx);
                }

                await tx.CommitAsync();
                return true;
            });
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

    public class BattleframeProgressionRow
    {
        public long id { get; set; }
        public int battleframe_sdb_id { get; set; }
        public int level { get; set; }
        public long current_xp { get; set; }
        public long lifetime_xp { get; set; }
        public int elite_level { get; set; }
        public long current_elite_xp { get; set; }
        public long lifetime_elite_xp { get; set; }
        public int elite_points { get; set; }
        public int bandwidth { get; set; }
        public List<BattleframePreviousUpgradeRow> previous_upgrades { get; set; } = new();
        public List<BattleframeAvailableUpgradeRow> available_upgrades { get; set; } = new();
    }

    public class BattleframePreviousUpgradeRow
    {
        public long battleframe_guid { get; set; }
        public int upgrade_id { get; set; }
        public int count { get; set; }
    }

    public class BattleframeAvailableUpgradeRow
    {
        public long battleframe_guid { get; set; }
        public int upgrade_index { get; set; }
        public int upgrade_id { get; set; }
        public int category { get; set; }
        public float stat_value { get; set; }
        public int? count { get; set; }
        public int? item_sdb_id { get; set; }
        public int localization_id { get; set; }
    }
}
