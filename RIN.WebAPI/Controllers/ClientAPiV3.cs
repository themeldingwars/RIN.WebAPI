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
        public async Task<IEnumerable<GarageSlot>> GarageSlots(long characterGuid)
        {
            return await Db.GetGarageSlots(characterGuid);
        }

        [HttpGet("garage_slots/battleframes_for_sale")]
        [R5SigAuthRequired]
        public async Task<object> BattleframesForSale()
        {
            return Content("{}", "application/json");
        }

        // The client calls this "visuals_v2": the garage posts { types = { warpaints, czi_patterns, decals,
        // visual_overrides } } when it opens, and the character customisation screen posts
        // { types = { head, head_accessory, ornaments } }. The body is ignored - every type is sent to both,
        // and each screen only reads the remote_types it knows.
        //
        // A product only has to carry remote_type, remote_id and prices: the client keys the response by
        // remote_type + remote_id and resolves the name, colours and texture out of its own copy of the sdb
        // (Game.GetWarpaintInfo / GetWarpaintPatternInfo / GetDecalInfo).
        [HttpPost("trade/products")]
        [R5SigAuthRequired]
        public async Task<List<TradeItem>> TradeProducts()
        {
            var items = new List<TradeItem>();

            void AddProduct(int remoteId, string remoteType, string name, int amount, int priceId)
            {
                items.Add(new TradeItem
                {
                    duration       = 0,
                    id             = remoteId,
                    remote_id      = remoteId,
                    name           = name,
                    quantity       = 0,
                    remote_type    = remoteType,
                    prices         = new[]
                    {
                        new TradePrice
                        {
                            amount             = amount,
                            currency_remote_id = 0,
                            currency_type      = "redbean",
                            id                 = priceId
                        }
                    },
                    unlock_context = "account"
                });
            }

            foreach (var info in await SDB.GetOrnamentsInfoList())
                AddProduct(info.id, "ornaments", info.lang_name, 0, 171201);

            foreach (var info in await SDB.GetWarpaintsInfoList())
                AddProduct(info.id, "warpaints", info.lang_name, 1, 0);

            foreach (var info in await SDB.GetCziPatternsInfoList())
                AddProduct(info.id, "czi_patterns", info.lang_name, 1, 0);

            foreach (var info in await SDB.GetTattooDecalsInfoList())
                AddProduct(info.id, "decals", info.lang_name, 1, 0);

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
