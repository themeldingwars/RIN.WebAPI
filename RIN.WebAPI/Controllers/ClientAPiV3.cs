using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RIN.Core;
using RIN.Core.Common;
using RIN.Core.DB;
using RIN.Core.DB.SDB;
using RIN.Core.Models.ClientApi;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Models.Config;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    [ApiController]
    [Route("Clientapi/api/v3")]
    [Produces("application/json")]
    [ProducesErrorResponseType(typeof(Error))]
    public partial class ClientAPiV3 : TmwController
    {
        private readonly ServerDefaultsSettings ServerDefaults;
        private readonly ILogger<OperatorController> Logger;
        private readonly DB Db;
        private readonly SDB SDB;

        public ClientAPiV3(IOptions<ServerDefaultsSettings> serverDefaults, ILogger<OperatorController> logger, DB db, SDB sdb)
        {
            ServerDefaults = serverDefaults.Value;
            Logger = logger;
            Db = db;
            SDB = sdb;
        }

        [HttpGet("characters/{characterGuid}/garage_slots")]
        [R5SigAuthRequired]
        public async Task<List<GarageSlot>> GarageSlots(long characterGuid)
        {
            var slots = new List<GarageSlot>()
            {
                new GarageSlot()
                {
                    id                = 0,
                    name              = "Crafting Station",
                    character_guid    = characterGuid,
                    garage_type       =  "crafting_station",
                    item_guid         = 0,
                    equipped_slots    = [],
                    limits            = new SlotLimits() { abilities = 4 },
                    decals            = new List<Decal>(),
                    visual_loadout_id = 0,
                    warpaint_id       = 0,
                    warpaintpatterns  = new List<WarpaintPattern>(),
                    visual_overrides  = new List<VisualOverride>(),
                    unlocked          = true,
                    expires_in_secs   = 0
                }
            };

            return slots;
        }

        [HttpPost("trade/products")]
        [R5SigAuthRequired]
        public async Task<List<TradeItem>> TradeProducts()
        {
            var cosmeticsInfos = await SDB.GetOrnamentsInfoList();
            var items = new List<TradeItem>();
            foreach (var info in cosmeticsInfos)
            {
                var tradeItem = new TradeItem
                {
                    duration    = 0,
                    id          = info.id,
                    remote_id   = info.id,
                    name        = info.lang_name,
                    quanity     = 1,
                    remote_type = "ornaments",
                    prices      = new[]
                    {
                        new TradePrice
                        {
                            amount             = 0,
                            currency_remote_id = 0,
                            currency_type      = "redbean",
                            id                 = 171201
                        }
                    },
                    unlock_context = "account"
                };

                items.Add(tradeItem);
            }

            return items;
        }

        // TODO: Return the proper perk data
        [HttpGet("trade/products/garage_slot_perk_respec")]
        [R5SigAuthRequired]
        public async Task<object> GarageSlotPerkRespec()
        {
            var data = """
                [{ "id": 577621, "name": "Perk Respec Points: 1", "remote_type": "garage_slot_perk_respec", "remote_id": 1, "quantity": 1,
                   "unlock_context": null, "duration": 0,
                   "prices": [{ "id": 1401522, "currency_type": "redbean", "currency_remote_id": 0, "amount": 2 }] }]
                """;

            return Content(data, "application/json");
        }

        [HttpGet("leaderboards/{leaderboardId}")]
        [R5SigAuthRequired]
        public async Task<Leaderboard?> GetLeaderboard(int leaderboardId, [FromQuery] int page = 1)
        {
            return await Db.GetLeaderboard(leaderboardId, page);
        }

        // TODO: Implement
        [HttpGet("trade/products/inventory_expansion")]
        [R5SigAuthRequired]
        public async Task<object> InventoryExpansion()
        {
            var data = "[]";

            return Content(data, "application/json");
        }

        // TODO: Implement
        [HttpGet("squad_builder/lfp")]
        [R5SigAuthRequired]
        public async Task<object> LookingForPeople()
        {
            var data = """{ "page": 1, "total_count": 0, "results": [] }""";

            return Content(data, "application/json");
        }

        [HttpGet("daily_rewards")]
        [R5SigAuthRequired]
        public async Task<Dictionary<string, List<DailyReward>>> DailyRewards()
        {
            var rewards = new Dictionary<string, List<DailyReward>>
            {
                ["1"] = [new DailyReward { item_type = 93675, quantity = 1 }],
                ["2"] = [new DailyReward { item_type = 96772, quantity = 1 }],
                ["3"] = [new DailyReward { item_type = 96771, quantity = 1 }],
                ["4"] = [new DailyReward { item_type = 96770, quantity = 1 }],
                ["5"] = [new DailyReward { item_type = 96769, quantity = 1 }]
            };

            return rewards;
        }

        [HttpPost("ui_actions")]
        [R5SigAuthRequired]
        public async Task<object> UiActions()
        {
            return Content("{}", "application/json");
        }
    }
}
