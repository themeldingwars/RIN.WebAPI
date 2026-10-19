using Microsoft.AspNetCore.Mvc;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientApiV4
    {
        // TODO: Implement
        [HttpGet("dashboard/conductor-assets")]
        [R5SigAuthRequired]
        public async Task<object> ConductorAssets()
        {
            var data = "[]";

            return Content(data, "application/json");
        }

        // TODO: Implement
        [HttpGet("dashboard/conductor-events")]
        [R5SigAuthRequired]
        public async Task<object> ConductorEvents()
        {
            var data = "[]";

            return Content(data, "application/json");
        }
    }
}
