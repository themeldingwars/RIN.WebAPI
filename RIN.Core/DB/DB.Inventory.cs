using Dapper;
using RIN.Core.Common;
using System.Linq;

namespace RIN.Core.DB
{
    public partial class DB
    {
        public async Task<(List<InventoryItemRow> items, List<InventoryResourceRow> resources, List<InventoryLoadoutRow> loadouts, List<InventoryLoadoutSlotRow> slots, List<InventoryLoadoutPerkRow> perks)> GetCharacterInventory(long charGuid)
        {
            const string SQL = @"
                SELECT item_guid, sdb_id, sub_inventory, dynamic_flags, durability
                FROM webapi.""CharacterItems""
                WHERE character_guid = @charGuid;

                SELECT m.item_guid, m.slot_index, m.module_sdb_id
                FROM webapi.""CharacterItemModules"" m
                INNER JOIN webapi.""CharacterItems"" i ON m.item_guid = i.item_guid
                WHERE i.character_guid = @charGuid;

                SELECT sdb_id, quantity, sub_inventory
                FROM webapi.""CharacterResources""
                WHERE character_guid = @charGuid;

                SELECT loadout_id, chassis_id, loadout_name, loadout_type
                FROM webapi.""CharacterLoadouts""
                WHERE character_guid = @charGuid;

                SELECT s.loadout_id, s.config_id, s.slot_index, s.item_guid
                FROM webapi.""CharacterLoadoutSlots"" s
                INNER JOIN webapi.""CharacterLoadouts"" l ON s.loadout_id = l.loadout_id
                WHERE l.character_guid = @charGuid;

                SELECT k.loadout_id, k.perk_item_sdb_id, k.level
                FROM webapi.""CharacterLoadoutPerks"" k
                INNER JOIN webapi.""CharacterLoadouts"" l ON k.loadout_id = l.loadout_id
                WHERE l.character_guid = @charGuid;";

            var result = await DBCall(async conn =>
            {
                using var multi = await conn.QueryMultipleAsync(SQL, new { charGuid });
                var items = (await multi.ReadAsync<InventoryItemRow>()).ToList();
                var itemModules = (await multi.ReadAsync<ItemModuleRow>()).ToList();
                var resources = (await multi.ReadAsync<InventoryResourceRow>()).ToList();
                var loadouts = (await multi.ReadAsync<InventoryLoadoutRow>()).ToList();
                var slots = (await multi.ReadAsync<InventoryLoadoutSlotRow>()).ToList();
                var perks = (await multi.ReadAsync<InventoryLoadoutPerkRow>()).ToList();

                var modulesByItem = itemModules.GroupBy(m => m.item_guid).ToDictionary(g => g.Key, g => g.ToList());
                var slottedItems  = new HashSet<long>(slots.Select(s => s.item_guid));

                foreach (var item in items)
                {
                    if (modulesByItem.TryGetValue(item.item_guid, out var itemModuleList))
                        item.modules = itemModuleList;

                    item.dynamic_flags = (short) (item.dynamic_flags & ~(int) ItemDynamicFlags.IsEquipped);
                    if (slottedItems.Contains(item.item_guid))
                        item.dynamic_flags |= (short) ItemDynamicFlags.IsEquipped;
                }

                return (items, resources, loadouts, slots, perks);
            });

            return result;
        }

        /// <summary>
        /// The character's garage slots for the client API: one per loadout, each naming the inventory item of
        /// the chassis that loadout is built on.
        /// </summary>
        public async Task<IEnumerable<GarageSlot>> GetGarageSlots(long characterGuid)
        {
            const string SELECT_SQL = @"
                SELECT l.loadout_id AS id,
                       coalesce((SELECT ci.item_guid
                                 FROM webapi.""CharacterItems"" ci
                                 WHERE ci.loadout_id = l.loadout_id), 0) AS item_guid
                FROM webapi.""CharacterLoadouts"" l
                WHERE l.character_guid = @characterGuid
                ORDER BY l.loadout_id";

            return await DBCall(async conn => await conn.QueryAsync<GarageSlot>(SELECT_SQL, new { characterGuid }));
        }

        public async Task<bool> SaveInventoryItem(long characterGuid, long itemGuid, int sdbId, short subInventory, short dynamicFlags, int durability, List<InventorySeedModule> modules)
        {
            // IsEquipped is derived on read, so drop it here
            dynamicFlags = (short) (dynamicFlags & ~(int) ItemDynamicFlags.IsEquipped);

            return await DBCall(async conn =>
            {
                await using var tx = await conn.BeginTransactionAsync();

                await conn.ExecuteAsync(@"INSERT INTO webapi.""CharacterItems"" (item_guid, character_guid, sdb_id, sub_inventory, dynamic_flags, durability)
                                          VALUES (@itemGuid, @characterGuid, @sdbId, @subInventory, @dynamicFlags, @durability)
                                          ON CONFLICT (item_guid)
                                          DO UPDATE SET sdb_id = EXCLUDED.sdb_id,
                                                        sub_inventory = EXCLUDED.sub_inventory,
                                                        dynamic_flags = EXCLUDED.dynamic_flags,
                                                        durability = EXCLUDED.durability",
                    new { itemGuid, characterGuid, sdbId, subInventory, dynamicFlags, durability }, tx);

                await conn.ExecuteAsync(@"DELETE FROM webapi.""CharacterItemModules""
                                          WHERE item_guid = @itemGuid",
                    new { itemGuid }, tx);

                if (modules != null && modules.Any())
                {
                    foreach (var module in modules)
                    {
                        await conn.ExecuteAsync(@"INSERT INTO webapi.""CharacterItemModules"" (item_guid, slot_index, module_sdb_id)
                                                  VALUES (@itemGuid, @slotIndex, @moduleSdbId)
                                                  ON CONFLICT (item_guid, slot_index)
                                                  DO UPDATE SET module_sdb_id = EXCLUDED.module_sdb_id",
                            new { itemGuid, slotIndex = module.SlotIndex, moduleSdbId = module.ModuleSdbId }, tx);
                    }
                }

                await tx.CommitAsync();
                return true;
            });
        }

        public async Task<bool> SaveItemModule(long characterGuid, long itemGuid, short slotIndex, int moduleSdbId)
        {
            return await DBCall(async conn =>
            {
                var itemExists = await conn.QueryFirstOrDefaultAsync<int>(@"SELECT 1
                                                                            FROM webapi.""CharacterItems""
                                                                            WHERE item_guid = @itemGuid
                                                                              AND character_guid = @characterGuid",
                    new { itemGuid, characterGuid });

                if (itemExists == 0)
                {
                    return false;
                }

                var affected = await conn.ExecuteAsync(@"INSERT INTO webapi.""CharacterItemModules"" (item_guid, slot_index, module_sdb_id)
                                                         VALUES (@itemGuid, @slotIndex, @moduleSdbId)
                                                         ON CONFLICT (item_guid, slot_index)
                                                         DO UPDATE SET module_sdb_id = EXCLUDED.module_sdb_id",
                    new { itemGuid, slotIndex, moduleSdbId });
                return affected >= 0;
            });
        }

        public async Task<bool> DeleteItemModule(long characterGuid, long itemGuid, short slotIndex)
        {
            var affected = await DBCall(conn => conn.ExecuteAsync(@"DELETE FROM webapi.""CharacterItemModules"" m
                                                                    USING webapi.""CharacterItems"" i
                                                                    WHERE m.item_guid = i.item_guid
                                                                      AND i.character_guid = @characterGuid
                                                                      AND m.item_guid = @itemGuid
                                                                      AND m.slot_index = @slotIndex",
                new { characterGuid, itemGuid, slotIndex }));
            return affected > 0;
        }

        public async Task<bool> SaveInventoryResource(long characterGuid, int sdbId, int quantity, short subInventory)
        {
            const string UPSERT_SQL = @"INSERT INTO webapi.""CharacterResources"" (character_guid, sdb_id, quantity, sub_inventory)
                                        VALUES (@characterGuid, @sdbId, @quantity, @subInventory)
                                        ON CONFLICT (character_guid, sdb_id)
                                        DO UPDATE SET quantity = EXCLUDED.quantity,
                                                      sub_inventory = EXCLUDED.sub_inventory";

            var affected = await DBCall(conn => conn.ExecuteAsync(UPSERT_SQL, new { characterGuid, sdbId, quantity, subInventory }));
            return affected >= 0;
        }

        public async Task<bool> DeleteInventoryItem(long characterGuid, long itemGuid)
        {
            const string DELETE_SQL = @"DELETE FROM webapi.""CharacterItems""
                                        WHERE item_guid = @itemGuid
                                          AND character_guid = @characterGuid
                                          AND loadout_id IS NULL";

            var affected = await DBCall(conn => conn.ExecuteAsync(DELETE_SQL, new { characterGuid, itemGuid }));
            return affected > 0;
        }

        public async Task<bool> SaveLoadoutSlot(long characterGuid, int loadoutId, short configId, short slotIndex, long itemGuid)
        {
            const string UPSERT_SQL = @"INSERT INTO webapi.""CharacterLoadoutSlots"" (loadout_id, config_id, slot_index, item_guid)
                                        VALUES (@loadoutId, @configId, @slotIndex, @itemGuid)
                                        ON CONFLICT (loadout_id, config_id, slot_index)
                                        DO UPDATE SET item_guid = EXCLUDED.item_guid";

            var affected = await DBCall(conn => conn.ExecuteAsync(UPSERT_SQL, new { loadoutId, configId, slotIndex, itemGuid }));
            return affected >= 0;
        }

        public async Task<bool> DeleteLoadoutSlot(long characterGuid, int loadoutId, short configId, short slotIndex)
    {
        const string DELETE_SQL = @"DELETE FROM webapi.""CharacterLoadoutSlots"" s
                                    USING webapi.""CharacterLoadouts"" l
                                    WHERE s.loadout_id = l.loadout_id
                                      AND l.character_guid = @characterGuid
                                      AND s.loadout_id = @loadoutId
                                      AND s.config_id = @configId
                                      AND s.slot_index = @slotIndex";

        var affected = await DBCall(conn => conn.ExecuteAsync(DELETE_SQL, new { characterGuid, loadoutId, configId, slotIndex }));
        return affected > 0;
    }

        /// <summary>
        /// Slots a perk module in a loadout.
        /// Unlike SaveLoadoutSlot this one is scoped to the character.
        /// </summary>
        public async Task<bool> SaveLoadoutPerk(long characterGuid, int loadoutId, int perkItemSdbId, short level)
        {
            const string UPSERT_SQL = @"INSERT INTO webapi.""CharacterLoadoutPerks"" (loadout_id, perk_item_sdb_id, level)
                                        SELECT @loadoutId, @perkItemSdbId, @level
                                        WHERE EXISTS (SELECT 1
                                                      FROM webapi.""CharacterLoadouts""
                                                      WHERE loadout_id = @loadoutId
                                                        AND character_guid = @characterGuid)
                                        ON CONFLICT (loadout_id, perk_item_sdb_id)
                                        DO UPDATE SET level = EXCLUDED.level";

            var affected = await DBCall(conn => conn.ExecuteAsync(UPSERT_SQL, new { loadoutId, perkItemSdbId, level, characterGuid }));
            return affected > 0;
        }

        public async Task<bool> DeleteLoadoutPerk(long characterGuid, int loadoutId, int perkItemSdbId)
        {
            const string DELETE_SQL = @"DELETE FROM webapi.""CharacterLoadoutPerks"" k
                                        USING webapi.""CharacterLoadouts"" l
                                        WHERE k.loadout_id = l.loadout_id
                                          AND l.character_guid = @characterGuid
                                          AND k.loadout_id = @loadoutId
                                          AND k.perk_item_sdb_id = @perkItemSdbId";

            var affected = await DBCall(conn => conn.ExecuteAsync(DELETE_SQL, new { characterGuid, loadoutId, perkItemSdbId }));
            return affected > 0;
        }

    public async Task<bool> SeedCharacterInventory(long characterGuid, List<InventorySeedItem> items, List<InventorySeedResource> resources, List<InventorySeedLoadout> loadouts, List<InventorySeedLoadoutSlot> slots)
        {
            return await DBCall(async conn =>
            {
                await using var tx = await conn.BeginTransactionAsync();

                if (items != null && items.Any())
                {
                    foreach (var item in items)
                    {
                        // Same rule as SaveInventoryItem: the IsEquipped bit is derived, not stored.
                        await conn.ExecuteAsync(@"INSERT INTO webapi.""CharacterItems"" (item_guid, character_guid, sdb_id, sub_inventory, dynamic_flags, durability)
                                                  VALUES (@itemGuid, @characterGuid, @sdbId, @subInventory, @dynamicFlags, @durability)",
                            new { item.ItemGuid, characterGuid, item.SdbId, item.SubInventory, dynamicFlags = (short) (item.DynamicFlags & ~(int) ItemDynamicFlags.IsEquipped), item.Durability }, tx);

                        if (item.Modules != null && item.Modules.Any())
                        {
                            foreach (var module in item.Modules)
                            {
                                await conn.ExecuteAsync(@"INSERT INTO webapi.""CharacterItemModules"" (item_guid, slot_index, module_sdb_id)
                                                          VALUES (@itemGuid, @slotIndex, @moduleSdbId)",
                                    new { item.ItemGuid, slotIndex = module.SlotIndex, moduleSdbId = module.ModuleSdbId }, tx);
                            }
                        }
                    }
                }

                if (resources != null && resources.Any())
                {
                    foreach (var res in resources)
                    {
                        await conn.ExecuteAsync(@"INSERT INTO webapi.""CharacterResources"" (character_guid, sdb_id, quantity, sub_inventory)
                                                  VALUES (@characterGuid, @sdbId, @quantity, @subInventory)
                                                  ON CONFLICT (character_guid, sdb_id) DO NOTHING",
                            new { characterGuid, res.SdbId, res.Quantity, res.SubInventory }, tx);
                    }
                }

                if (loadouts != null && loadouts.Any())
                {
                    foreach (var loadout in loadouts)
                    {
                        await conn.ExecuteAsync(@"INSERT INTO webapi.""CharacterLoadouts"" (loadout_id, character_guid, chassis_id, loadout_name, loadout_type)
                                                  VALUES (@loadoutId, @characterGuid, @chassisId, @loadoutName, @loadoutType)",
                            new { loadout.LoadoutId, characterGuid, loadout.ChassisId, loadout.LoadoutName, loadout.LoadoutType }, tx);
                    }
                }

                if (slots != null && slots.Any())
                {
                    foreach (var slot in slots)
                    {
                        await conn.ExecuteAsync(@"INSERT INTO webapi.""CharacterLoadoutSlots"" (loadout_id, config_id, slot_index, item_guid)
                                                  VALUES (@loadoutId, @configId, @slotIndex, @itemGuid)
                                                  ON CONFLICT (loadout_id, config_id, slot_index) DO NOTHING",
                            new { slot.LoadoutId, slot.ConfigId, slot.SlotIndex, slot.ItemGuid }, tx);
                    }
                }

                await tx.CommitAsync();
                return true;
            });
        }

        /// <summary>
        /// Seeds a character from one of the webapi."NewCharacterTemplates" rows: items, resources,
        /// loadouts, loadout slots and a battleframe progression row per chassis. A no-op if the
        /// character already has items, and never overwrites a battleframe the character already has.
        /// templateId 0 selects the template flagged is_default.
        /// </summary>
        public async Task<bool> SeedCharacterFromTemplate(long characterGuid, int templateId)
        {
            const string CALL_SQL = @"SELECT webapi.""seed_character_inventory""(@characterGuid, @templateId)";

            return await DBCall(conn => conn.ExecuteAsync(CALL_SQL, new { characterGuid, templateId })) > 0;
        }

        public async Task<long?> AddInventoryItem(long characterGuid, int sdbId)
        {
            const string INSERT_SQL = @"INSERT INTO webapi.""CharacterItems"" (item_guid, character_guid, sdb_id, sub_inventory, dynamic_flags, durability)
                                        VALUES (webapi.create_entity_guid(253), @characterGuid, @sdbId, 1, 0, 1000)
                                        RETURNING item_guid";

            return await DBCall(conn => conn.QueryFirstOrDefaultAsync<long?>(INSERT_SQL, new { characterGuid, sdbId }));
        }

        public async Task<bool> ConsumeInventoryItem(long characterGuid, int sdbId, int quantity)
        {
            return await DBCall(async conn =>
            {
                await using var tx = await conn.BeginTransactionAsync();

                foreach (var itemGuid in await conn.QueryAsync<long>(@"SELECT item_guid
                                                                        FROM webapi.""CharacterItems""
                                                                        WHERE character_guid = @characterGuid
                                                                          AND sdb_id = @sdbId
                                                                        ORDER BY item_guid
                                                                        LIMIT @quantity",
                    new { characterGuid, sdbId, quantity }, tx))
                {
                    await conn.ExecuteAsync(@"DELETE FROM webapi.""CharacterItems""
                                              WHERE item_guid = @itemGuid AND character_guid = @characterGuid",
                        new { itemGuid, characterGuid }, tx);
                }

                await tx.CommitAsync();
                return true;
            });
        }

        public async Task<bool> ConsumeInventoryResource(long characterGuid, int sdbId, int quantity)
        {
            return await DBCall(async conn =>
            {
                await using var tx = await conn.BeginTransactionAsync();

                var currentQuantity = await conn.QueryFirstOrDefaultAsync<int?>(@"SELECT quantity
                                                                                FROM webapi.""CharacterResources""
                                                                                WHERE character_guid = @characterGuid
                                                                                  AND sdb_id = @sdbId",
                    new { characterGuid, sdbId }, tx);

                if (!currentQuantity.HasValue || currentQuantity.Value < quantity)
                {
                    return false;
                }

                await conn.ExecuteAsync(@"UPDATE webapi.""CharacterResources""
                                          SET quantity = quantity - @quantity
                                          WHERE character_guid = @characterGuid
                                            AND sdb_id = @sdbId",
                    new { characterGuid, sdbId, quantity }, tx);

                await tx.CommitAsync();
                return true;
            });
        }

        public async Task NotifyInventoryUpdated(long characterGuid)
        {
            await DBCall(async conn =>
            {
                await conn.ExecuteAsync(@"SELECT pg_notify('events', 'InventoryUpdated->' || json_build_object('CharacterGuid', @characterGuid)::text);",
                    new { characterGuid });
                return 0;
            });
        }

        public async Task<bool> SaveCharacterLoadout(long characterGuid, int loadoutId, int chassisId, PlayerBattleframeVisuals visuals, List<InventoryLoadoutSlotRow> slottedItems)
        {
            return await DBCall(async conn =>
            {
                await using var tx = await conn.BeginTransactionAsync();

                var bfId = await conn.QueryFirstOrDefaultAsync<long?>(@"SELECT id
                                                                          FROM webapi.""Battleframes""
                                                                          WHERE character_guid = @characterGuid
                                                                            AND battleframe_sdb_id = @chassisId",
                    new { characterGuid, chassisId }, tx);

                if (!bfId.HasValue || bfId.Value <= 0)
                {
                    bfId = await conn.QueryFirstOrDefaultAsync<long?>(@"INSERT INTO webapi.""Battleframes"" (id, character_guid, battleframe_sdb_id, visuals, hidden, level, current_xp, lifetime_xp, elite_level, current_elite_xp, lifetime_elite_xp)
                                                                          VALUES (webapi.create_entity_guid(253), @characterGuid, @chassisId, @visuals, false, 1, 0, 0, 0, 0, 0)
                                                                          RETURNING id",
                        new { characterGuid, chassisId, visuals = Utils.MiscUtils.ToProtoBuffByteArray(visuals) }, tx);
                }
                else
                {
                    await conn.ExecuteAsync(@"UPDATE webapi.""Battleframes""
                                              SET visuals = @visuals
                                              WHERE id = @bfId",
                        new { visuals = Utils.MiscUtils.ToProtoBuffByteArray(visuals), bfId = bfId.Value }, tx);
                }

                await conn.ExecuteAsync(@"INSERT INTO webapi.""CharacterLoadouts"" (loadout_id, character_guid, chassis_id)
                                          VALUES (@loadoutId, @characterGuid, @chassisId)
                                          ON CONFLICT (loadout_id)
                                          DO UPDATE SET chassis_id = EXCLUDED.chassis_id",
                    new { loadoutId, characterGuid, chassisId }, tx);

                if (slottedItems != null && slottedItems.Any())
                {
                    await conn.ExecuteAsync(@"DELETE FROM webapi.""CharacterLoadoutSlots""
                                              WHERE loadout_id = @loadoutId",
                        new { loadoutId }, tx);

                    foreach (var slot in slottedItems)
                    {
                        await conn.ExecuteAsync(@"INSERT INTO webapi.""CharacterLoadoutSlots"" (loadout_id, config_id, slot_index, item_guid)
                                                  VALUES (@loadoutId, @configId, @slotIndex, @itemGuid)
                                                  ON CONFLICT (loadout_id, config_id, slot_index)
                                                  DO UPDATE SET item_guid = EXCLUDED.item_guid",
                            new { loadoutId, slot.config_id, slot.slot_index, slot.item_guid }, tx);
                    }
                }

                await tx.CommitAsync();
                return true;
            });
        }
    }

    public class InventoryItemRow
    {
        public long item_guid { get; set; }
        public int sdb_id { get; set; }
        public short sub_inventory { get; set; }
        public short dynamic_flags { get; set; }
        public int durability { get; set; }
        public List<ItemModuleRow> modules { get; set; } = new();
    }

    public class ItemModuleRow
    {
        public long item_guid { get; set; }
        public short slot_index { get; set; }
        public int module_sdb_id { get; set; }
    }

    public class InventoryResourceRow
    {
        public int sdb_id { get; set; }
        public int quantity { get; set; }
        public short sub_inventory { get; set; }
    }

    public class InventoryLoadoutRow
    {
        public int loadout_id { get; set; }
        public int chassis_id { get; set; }
        public string loadout_name { get; set; } = string.Empty;
        public string loadout_type { get; set; } = string.Empty;
    }

    public class InventoryLoadoutPerkRow
    {
        public int loadout_id { get; set; }
        public int perk_item_sdb_id { get; set; }
        public short level { get; set; }
    }

    public class InventoryLoadoutSlotRow
    {
        public int loadout_id { get; set; }
        public short config_id { get; set; }
        public short slot_index { get; set; }
        public long item_guid { get; set; }
    }

    public class InventorySeedItem
    {
        public long ItemGuid { get; set; }
        public int SdbId { get; set; }
        public short SubInventory { get; set; }
        public short DynamicFlags { get; set; }
        public int Durability { get; set; }
        public List<InventorySeedModule> Modules { get; set; } = new();
    }

    public class InventorySeedModule
    {
        public short SlotIndex { get; set; }
        public int ModuleSdbId { get; set; }
    }

    public class InventorySeedResource
    {
        public int SdbId { get; set; }
        public int Quantity { get; set; }
        public short SubInventory { get; set; }
    }
    public class InventorySeedLoadout
    {
        public int LoadoutId { get; set; }
        public int ChassisId { get; set; }
        public string LoadoutName { get; set; } = string.Empty;
        public string LoadoutType { get; set; } = string.Empty;
    }
    public class InventorySeedLoadoutSlot
    {
        public int LoadoutId { get; set; }
        public short ConfigId { get; set; }
        public short SlotIndex { get; set; }
        public long ItemGuid { get; set; }
    }
}
