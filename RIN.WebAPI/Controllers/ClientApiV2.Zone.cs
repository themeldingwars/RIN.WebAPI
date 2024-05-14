using Microsoft.AspNetCore.Mvc;
using RIN.Core.Models.ClientApi;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientApiV2
    {
        [HttpGet("zone_settings")]
        [R5SigAuthRequired]
        public async Task<List<ZoneSettings>> ZoneSettings()
        {
            return await Db.GetZoneSettings();
        }
    }
}
