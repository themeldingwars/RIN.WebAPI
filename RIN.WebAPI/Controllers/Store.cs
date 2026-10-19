using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RIN.Core.DB;
using RIN.Core.DB.SDB;
using RIN.WebAPI.Models.Config;
using RIN.WebAPI.Models.Store;

namespace RIN.WebAPI.Controllers
{
    [ApiController]
    [Route("Store")]
    public partial class Store : TmwController
    {
        private readonly ServerDefaultsSettings         ServerDefaults;
        private readonly ILogger<OperatorController>    Logger;
        private readonly DB                             Db;
        private readonly SDB                            SDB;

        public Store(IOptions<ServerDefaultsSettings> serverDefaults, ILogger<OperatorController> logger, DB db, SDB sdb)
        {
            ServerDefaults = serverDefaults.Value;
            Logger         = logger;
            Db             = db;
            SDB            = sdb;
        }

        // TODO: Implement
        [HttpGet("products.json")]
        public async Task<object> Products()
        {
            // Products that are not active and approved are ignored. There must be 5 featured products to avoid errors after opening the menu/dashboard    .
            var resp = new List<Product>();
            resp.Add(new Product() { active = true, approved = true, featured = true, name = "Ad 1", best_discount = 0, lowest_price = 1, overview_image = "" });
            resp.Add(new Product() { active = true, approved = true, featured = true, name = "Ad 2", best_discount = 0, lowest_price = 1, overview_image = "" });
            resp.Add(new Product() { active = true, approved = true, featured = true, name = "Ad 3", best_discount = 0, lowest_price = 1, overview_image = "" });
            resp.Add(new Product() { active = true, approved = true, featured = true, name = "Ad 4", best_discount = 0, lowest_price = 1, overview_image = "" });
            resp.Add(new Product() { active = true, approved = true, featured = true, name = "Ad 5", best_discount = 0, lowest_price = 1, overview_image = "" });

            return resp;
        }
    }
}