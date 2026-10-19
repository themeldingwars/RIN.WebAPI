using System.Threading.Channels;
using ProtoBuf.Grpc;
using RIN.Core.DB;
using RIN.Core.Models.DB;
using RIN.InternalAPI.Models;

namespace RIN.InternalAPI.Services
{
    public class GameServerAPI : IGameServerAPI
    {
        private readonly DB Db;
        private readonly ILogger<GameServerAPI> Logger;
        private readonly DbEventBus EventBus;
        private readonly IHostApplicationLifetime Lifetime;

        public GameServerAPI(DB db, ILogger<GameServerAPI> logger, DbEventBus eventBus, IHostApplicationLifetime lifetime)
        {
            Db     = db;
            Logger = logger;
            EventBus = eventBus;
            Lifetime = lifetime;
        }

        public async ValueTask<PingResp> Ping(PingReq req)
        {
            var resp = new PingResp
            {
                ClientSentTime   = req.SentTime,
                ServerReciveTime = DateTime.UtcNow
            };

            return resp;
        }

        /// <summary>
        /// The login gate. Checks the ticket payload the client delivered (35 bytes, "R5 " header included)
        /// and, when a character was asked for, that the character belongs to that ticket's account. PIN
        /// passes the bytes through untouched - only RIN generated them, so only RIN can check them. The
        /// ticket is never logged.
        /// </summary>
        public async ValueTask<LoginAuthorizationResponse> AuthorizeLogin(LoginAuthorizationRequest req)
        {
            var resp = new LoginAuthorizationResponse
            {
                Reason        = LoginAuthorizationReasons.DB_ERROR,
                CharacterGuid = req.CharacterGuid
            };

            var check = await Db.ValidateLoginTicket(req.Ticket);
            if (check == null || check.Status != LoginTicketStatus.Valid) {
                resp.Reason = check?.Status switch {
                    LoginTicketStatus.Malformed => LoginAuthorizationReasons.TICKET_MALFORMED,
                    LoginTicketStatus.Unknown   => LoginAuthorizationReasons.TICKET_UNKNOWN,
                    _                           => LoginAuthorizationReasons.DB_ERROR
                };

                Logger.LogWarning("AuthorizeLogin: rejected a login attempt ({reason})", resp.Reason);
                return resp;
            }

            var account = check.Account;

            resp.Authorized     = true;
            resp.Reason         = LoginAuthorizationReasons.OK;
            resp.AccountId      = account.account_id;
            resp.IsDev          = account.is_dev;
            resp.CharacterLimit = account.character_limit;

            // CharacterGuid 0 means "authorise the ticket only", before a character has been picked.
            if (req.CharacterGuid == 0)
                return resp;

            var character = await Db.GetLoginCharacter((long) req.CharacterGuid);
            if (character == null) {
                resp.Authorized = false;
                resp.Reason     = LoginAuthorizationReasons.CHARACTER_NOT_FOUND;
                Logger.LogWarning("AuthorizeLogin: account {accountId} asked for unknown character {characterGuid}",
                                  account.account_id, req.CharacterGuid);
                return resp;
            }

            resp.CharacterGuid  = (ulong) character.character_guid;
            resp.CharacterName  = character.name;
            resp.CharacterIsDev = character.is_dev;

            if (character.account_id != account.account_id) {
                resp.Authorized = false;
                resp.Reason     = LoginAuthorizationReasons.CHARACTER_NOT_ON_ACCOUNT;
                Logger.LogWarning("AuthorizeLogin: character {characterGuid} belongs to account {owner}, not {accountId}",
                                  character.character_guid, character.account_id, account.account_id);
                return resp;
            }

            if (character.is_deleted) {
                resp.Authorized = false;
                resp.Reason     = LoginAuthorizationReasons.CHARACTER_DELETED;
                Logger.LogWarning("AuthorizeLogin: account {accountId} asked for deleted character {characterGuid}",
                                  account.account_id, character.character_guid);
                return resp;
            }

            return resp;
        }

        public async ValueTask<CharacterAndBattleframeVisuals> GetCharacterAndBattleframeVisuals(CharacterID req)
        {
            var result    = await Db.GetBasicCharacterAndVisualData(req.ID);
            var bfVisuals = PlayerBattleframeVisuals.CreateDefault();

            var resp = new CharacterAndBattleframeVisuals
            {
                CharacterInfo      = result.info,
                CharacterVisuals   = result.visuals,
                BattleframeVisuals = bfVisuals
            };

            return resp;
        }

        public async ValueTask<CharacterUnlocksResponse> GetCharacterUnlocks(CharacterID req)
        {
            var dbUnlocks = await Db.GetCharacterUnlocks(req.ID);

            var resp = new CharacterUnlocksResponse();

            foreach (var unlock in dbUnlocks)
            {
                resp.Unlocks.Add(new CharacterUnlockEntry
                {
                    UnlockType = unlock.unlockType,
                    UnlockId   = (uint)unlock.unlockId,
                    FrameId    = (uint)unlock.frameId
                });
            }

            return resp;
        }

        public async ValueTask<BattleframeProgressionResponse> GetBattleframeProgression(CharacterID req)
        {
            var dbProgression = await Db.GetBattleframeProgression(req.ID);

            var resp = new BattleframeProgressionResponse();

            foreach (var battleframe in dbProgression)
            {
                resp.Progression.Add(new BattleframeProgressionEntry
                {
                    BattleframeSdbId = (uint)battleframe.battleframe_sdb_id,
                    BattleframeGuid  = (ulong)battleframe.id,
                    Level            = (uint)battleframe.level,
                    CurrentXp        = (ulong)battleframe.current_xp,
                    LifetimeXp       = (ulong)battleframe.lifetime_xp,
                    EliteLevel       = (uint)battleframe.elite_level,
                    CurrentEliteXp   = (ulong)battleframe.current_elite_xp,
                    LifetimeEliteXp  = (ulong)battleframe.lifetime_elite_xp,
                    ElitePoints      = (uint)battleframe.elite_points,
                    Bandwidth        = (uint)battleframe.bandwidth,
                    PreviousUpgrades = battleframe.previous_upgrades.Select(x => new BattleframeUpgradeEntry
                    {
                        UpgradeId = (uint)x.upgrade_id,
                        Count     = (uint)x.count
                    }).ToList(),
                    AvailableUpgrades = battleframe.available_upgrades.Select(x => new BattleframeAvailableUpgradeEntry
                    {
                        UpgradeIndex   = (uint)x.upgrade_index,
                        UpgradeId      = (uint)x.upgrade_id,
                        Category       = (uint)x.category,
                        StatValue      = x.stat_value,
                        Count          = x.count.HasValue ? (uint?)x.count.Value : null,
                        ItemSdbId      = x.item_sdb_id.HasValue ? (uint?)x.item_sdb_id.Value : null,
                        LocalizationId = (uint)x.localization_id
                    }).ToList()
                });
            }

            return resp;
        }

        public async ValueTask<TransferCharacterResp> TransferCharacter(TransferCharacterReq req)
        {
            var saved = await Db.UpdateCharacterAfterGameSession((long)req.CharacterId, (int)req.ZoneId, (int)req.OutpostId, (int)req.TimePlayed);

            return new TransferCharacterResp { Success = saved };
        }

        public async ValueTask<CharacterInventoryResponse> GetCharacterInventory(CharacterID req)
        {
            var dbInventory = await Db.GetCharacterInventory(req.ID);

            var resp = new CharacterInventoryResponse();

            foreach (var item in dbInventory.items)
            {
                resp.Items.Add(new CharacterItem
                {
                    Guid         = (ulong)item.item_guid,
                    SdbId        = (uint)item.sdb_id,
                    SubInventory = (uint)item.sub_inventory,
                    DynamicFlags = (uint)item.dynamic_flags,
                    Durability   = (uint)item.durability,
                    Modules      = item.modules.Select(x => new ItemModule
                    {
                        SlotIndex   = (uint)x.slot_index,
                        ModuleSdbId = (uint)x.module_sdb_id
                    }).ToList(),
                });
            }

            foreach (var res in dbInventory.resources)
            {
                resp.Resources.Add(new CharacterResource
                {
                    SdbId        = (uint)res.sdb_id,
                    Quantity     = (uint)res.quantity,
                    SubInventory = (uint)res.sub_inventory,
                });
            }

            foreach (var loadout in dbInventory.loadouts)
            {
                resp.Loadouts.Add(new CharacterLoadout
                {
                    LoadoutId   = loadout.loadout_id,
                    ChassisId   = loadout.chassis_id,
                    LoadoutName = loadout.loadout_name,
                    LoadoutType = loadout.loadout_type,
                });
            }

            foreach (var slot in dbInventory.slots)
            {
                resp.Slots.Add(new CharacterLoadoutSlotEntry
                {
                    LoadoutId = slot.loadout_id,
                    ConfigId  = (uint)slot.config_id,
                    SlotIndex = (uint)slot.slot_index,
                    ItemGuid  = slot.item_guid,
                });
            }

            foreach (var perk in dbInventory.perks)
            {
                resp.LoadoutPerks.Add(new CharacterLoadoutPerkEntry
                {
                    LoadoutId     = perk.loadout_id,
                    PerkItemSdbId = (uint)perk.perk_item_sdb_id,
                    Level         = (uint)perk.level,
                });
            }

            // Visual sets come back exactly as they were stored: same order, same floats. Any conversion for a
            // game client or the web API belongs on the side that needs it, not here.
            foreach (var set in dbInventory.visuals)
            {
                resp.Visuals.Add(new CharacterLoadoutVisualsEntry
                {
                    LoadoutId = set.loadout_id,
                    ConfigId  = (uint)set.config_id,
                    Visuals   = set.visuals.Select(ToWireVisual).ToList(),
                });
            }

            return resp;
        }

        public async IAsyncEnumerable<Event> Stream(IAsyncEnumerable<Command> commands, CallContext context = default)
        {
            var channel = Channel.CreateUnbounded<Event>();
            var subscriptionId = EventBus.Subscribe(channel);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, Lifetime.ApplicationStopping);
            var token = cts.Token;
            var commandsTask = ReadCommands(commands, channel, token);

            try
            {
                while (true)
                {
                    Event evt;
                    try
                    {
                        evt = await channel.Reader.ReadAsync(token);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException and not ChannelClosedException)
                        {
                            Logger.LogError(ex, "GRPC Stream crashed");
                        }

                        break;
                    }

                    yield return evt;
                }
            }
            finally
            {
                await cts.CancelAsync();
                channel.Writer.TryComplete();
                EventBus.Unsubscribe(subscriptionId);
                await commandsTask;
            }
        }

        private async Task ReadCommands(IAsyncEnumerable<Command> commands, Channel<Event> channel, CancellationToken token)
        {
            try
            {
                await foreach (var command in commands.WithCancellation(token))
                {
                    Logger.LogInformation("Received command: {command}", command);

                    switch (command)
                    {
                        case SaveGameSessionData data:
                            await Db.UpdateCharacterAfterGameSession((long)data.CharacterId, (int)data.ZoneId, (int)data.OutpostId, (int)data.TimePlayed);
                            break;
                        case SaveLgvRaceFinish race:
                            await Db.SaveLgvRaceFinish((long)race.CharacterGuid, (int)race.LeaderboardId, (long)race.TimeMs);
                            break;
                        case SaveCharacterUnlock unlock:
                            await Db.UpsertCharacterUnlock((long)unlock.CharacterGuid, unlock.UnlockType, (int)unlock.UnlockId, (int)unlock.FrameId);
                            break;
                        case DeleteCharacterUnlock unlock:
                            await Db.DeleteCharacterUnlock((long)unlock.CharacterGuid, unlock.UnlockType, (int)unlock.UnlockId, (int)unlock.FrameId);
                            break;
                        case SaveInventoryItem item:
                            await Db.SaveInventoryItem(
                                (long)item.CharacterGuid,
                                (long)item.ItemGuid,
                                (int)item.SdbId,
                                (short)item.SubInventory,
                                (short)item.DynamicFlags,
                                (int)item.Durability,
                                item.Modules?.Select(x => new InventorySeedModule
                                {
                                    SlotIndex   = (short)x.SlotIndex,
                                    ModuleSdbId = (int)x.ModuleSdbId
                                }).ToList() ?? new List<InventorySeedModule>());
                            await Db.NotifyInventoryUpdated((long)item.CharacterGuid);
                            break;
                        case SaveItemModule saveModule:
                            await Db.SaveItemModule(
                                (long)saveModule.CharacterGuid,
                                (long)saveModule.ItemGuid,
                                (short)saveModule.SlotIndex,
                                (int)saveModule.ModuleSdbId);
                            await Db.NotifyInventoryUpdated((long)saveModule.CharacterGuid);
                            break;
                        case DeleteItemModule deleteModule:
                            await Db.DeleteItemModule(
                                (long)deleteModule.CharacterGuid,
                                (long)deleteModule.ItemGuid,
                                (short)deleteModule.SlotIndex);
                            await Db.NotifyInventoryUpdated((long)deleteModule.CharacterGuid);
                            break;
                        case SaveInventoryResource resource:
                            await Db.SaveInventoryResource(
                                (long)resource.CharacterGuid,
                                (int)resource.SdbId,
                                (int)resource.Quantity,
                                (short)resource.SubInventory);
                            await Db.NotifyInventoryUpdated((long)resource.CharacterGuid);
                            break;
                        case SeedCharacterInventory seed:
                            var items = seed.Items?.Select(x => new InventorySeedItem
                            {
                                ItemGuid = (long)x.ItemGuid,
                                SdbId = (int)x.SdbId,
                                SubInventory = (short)x.SubInventory,
                                DynamicFlags = (short)x.DynamicFlags,
                                Durability = (int)x.Durability,
                                Modules = x.Modules?.Select(m => new InventorySeedModule
                                {
                                    SlotIndex   = (short)m.SlotIndex,
                                    ModuleSdbId = (int)m.ModuleSdbId
                                }).ToList() ?? new List<InventorySeedModule>()
                            }).ToList() ?? new List<InventorySeedItem>();
                            var resources = seed.Resources?.Select(x => new InventorySeedResource
                            {
                                SdbId = (int)x.SdbId,
                                Quantity = (int)x.Quantity,
                                SubInventory = (short)x.SubInventory
                            }).ToList() ?? new List<InventorySeedResource>();
                            var loadouts = seed.Loadouts?.Select(x => new InventorySeedLoadout
                            {
                                LoadoutId = x.LoadoutId,
                                ChassisId = (int)x.ChassisId,
                                LoadoutName = x.LoadoutName,
                                LoadoutType = x.LoadoutType
                            }).ToList() ?? new List<InventorySeedLoadout>();
                            var slots = seed.Slots?.Select(x => new InventorySeedLoadoutSlot
                            {
                                LoadoutId = x.LoadoutId,
                                ConfigId = (short)x.ConfigId,
                                SlotIndex = (short)x.SlotIndex,
                                ItemGuid = (long)x.ItemGuid
                            }).ToList() ?? new List<InventorySeedLoadoutSlot>();
                            var seedVisuals = seed.Visuals?.Select(x => new InventorySeedLoadoutVisual
                            {
                                LoadoutId = x.LoadoutId,
                                ConfigId = (short)x.ConfigId,
                                Visuals = x.Visuals?.Select(ToStoredVisual).ToList() ?? new List<LoadoutVisualEntry>()
                            }).ToList() ?? new List<InventorySeedLoadoutVisual>();
                            await Db.SeedCharacterInventory((long)seed.CharacterGuid, items, resources, loadouts, slots, seedVisuals);
                            await Db.NotifyInventoryUpdated((long)seed.CharacterGuid);
                            break;
                        case SeedCharacterFromTemplate seedTemplate:
                            await Db.SeedCharacterFromTemplate((long)seedTemplate.CharacterGuid, (int)seedTemplate.TemplateId);
                            await Db.NotifyInventoryUpdated((long)seedTemplate.CharacterGuid);
                            break;
                        case DeleteInventoryItem delItem:
                            await Db.DeleteInventoryItem((long)delItem.CharacterGuid, (long)delItem.ItemGuid);
                            await Db.NotifyInventoryUpdated((long)delItem.CharacterGuid);
                            break;
                        case SaveLoadoutSlot slot:
                            await Db.SaveLoadoutSlot(
                                (long)slot.CharacterGuid,
                                slot.LoadoutId,
                                (short)slot.ConfigId,
                                (short)slot.SlotIndex,
                                (long)slot.ItemGuid);
                            await Db.NotifyInventoryUpdated((long)slot.CharacterGuid);
                            break;
                        case DeleteLoadoutSlot delSlot:
                            await Db.DeleteLoadoutSlot(
                                (long)delSlot.CharacterGuid,
                                delSlot.LoadoutId,
                                (short)delSlot.ConfigId,
                                (short)delSlot.SlotIndex);
                            await Db.NotifyInventoryUpdated((long)delSlot.CharacterGuid);
                            break;
                        case SaveCharacterLoadout loadout:
                            await Db.SaveCharacterLoadout(
                                (long)loadout.CharacterGuid,
                                loadout.LoadoutId,
                                loadout.ChassisId,
                                loadout.Visuals,
                                loadout.SlottedItems?.Select(x => new InventoryLoadoutSlotRow
                                {
                                    loadout_id = x.LoadoutId,
                                    config_id = (short)x.ConfigId,
                                    slot_index = (short)x.SlotIndex,
                                    item_guid = x.ItemGuid
                                }).ToList() ?? new List<InventoryLoadoutSlotRow>());
                            await Db.NotifyInventoryUpdated((long)loadout.CharacterGuid);
                            break;
                        case SaveCurrentBattleframe bf:
                            await Db.SetCharacterCurrentBattleframeBySdbId((long)bf.CharacterGuid, bf.ChassisId);
                            break;
                        case SaveBattleframeXp xp:
                            await Db.UpdateBattleframeXp((long)xp.CharacterGuid, (int)xp.BattleframeSdbId, (int)xp.Level, (long)xp.CurrentXp, (long)xp.LifetimeXp);
                            break;
                        case SaveBattleframeEliteXp xp:
                            await Db.UpdateBattleframeEliteXp((long)xp.CharacterGuid, (int)xp.BattleframeSdbId, (int)xp.EliteLevel, (long)xp.CurrentEliteXp, (long)xp.LifetimeEliteXp, (int)xp.ElitePoints);
                            break;
                        case SaveBattleframeUpgrade upgrade:
                            await Db.SaveBattleframeUpgrade((long)upgrade.CharacterGuid, (int)upgrade.BattleframeSdbId, (int)upgrade.UpgradeId, (int)upgrade.Count);
                            break;
                        case ClearBattleframeUpgrades clear:
                            await Db.ClearBattleframeUpgrades((long)clear.CharacterGuid, (int)clear.BattleframeSdbId);
                            break;
                        case SaveBattleframeAvailableUpgrades rolled:
                            await Db.SaveBattleframeAvailableUpgrades(
                                (long)rolled.CharacterGuid,
                                (int)rolled.BattleframeSdbId,
                                rolled.Upgrades?.Select(x => new BattleframeAvailableUpgradeRow
                                {
                                    upgrade_index   = (int)x.UpgradeIndex,
                                    upgrade_id      = (int)x.UpgradeId,
                                    category        = (int)x.Category,
                                    stat_value      = x.StatValue,
                                    count           = x.Count.HasValue ? (int?)x.Count.Value : null,
                                    item_sdb_id     = x.ItemSdbId.HasValue ? (int?)x.ItemSdbId.Value : null,
                                    localization_id = (int)x.LocalizationId
                                }).ToList() ?? new List<BattleframeAvailableUpgradeRow>());
                            break;
                        case SaveLoadoutPerk perk:
                            await Db.SaveLoadoutPerk(
                                (long)perk.CharacterGuid,
                                perk.LoadoutId,
                                (int)perk.PerkItemSdbId,
                                (short)perk.Level);
                            await Db.NotifyInventoryUpdated((long)perk.CharacterGuid);
                            break;
                        case DeleteLoadoutPerk delPerk:
                            await Db.DeleteLoadoutPerk((long)delPerk.CharacterGuid, delPerk.LoadoutId, (int)delPerk.PerkItemSdbId);
                            await Db.NotifyInventoryUpdated((long)delPerk.CharacterGuid);
                            break;
                        case SaveBattleframeBandwidth bandwidth:
                            await Db.UpdateBattleframeBandwidth((long)bandwidth.CharacterGuid, (int)bandwidth.BattleframeSdbId, (int)bandwidth.Bandwidth);
                            break;
                        case SaveLoadoutVisuals visual:
                            await Db.SaveLoadoutVisuals(
                                (long)visual.CharacterGuid,
                                visual.LoadoutId,
                                (short)visual.ConfigId,
                                visual.Visuals?.Select(ToStoredVisual).ToList() ?? new List<LoadoutVisualEntry>());
                            await Db.NotifyInventoryUpdated((long)visual.CharacterGuid);
                            break;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log here as well, as it runs in a separate task and won't be caught by the main loop
                Logger.LogError(ex, "GRPC Stream crashed");
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }

        // The wire shape and the stored shape of a visual are the same fields; these two only move them across
        // the assembly boundary. Nothing is converted, clamped or reordered in either direction.
        private static LoadoutVisual ToWireVisual(LoadoutVisualEntry entry)
        {
            return new LoadoutVisual
            {
                VisualType = entry.VisualType,
                ItemSdbId  = entry.ItemSdbId,
                Data1      = entry.Data1,
                Data2      = entry.Data2,
                Transform  = entry.Transform ?? new List<float>()
            };
        }

        private static LoadoutVisualEntry ToStoredVisual(LoadoutVisual visual)
        {
            return new LoadoutVisualEntry
            {
                VisualType = visual.VisualType,
                ItemSdbId  = visual.ItemSdbId,
                Data1      = visual.Data1,
                Data2      = visual.Data2,
                Transform  = visual.Transform ?? new List<float>()
            };
        }
    }
}
