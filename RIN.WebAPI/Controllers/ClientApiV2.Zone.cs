using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientApiV2
    {
        [HttpGet("zone_settings")]
        [R5SigAuthRequired]
        public async Task<IActionResult> ZoneSettings()
        {
            var response = await Db.GetZoneSettings();

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            };

            return new JsonResult(response, options);
        }

        [HttpGet("zone_settings/context/{context}")]
        [R5SigAuthRequired]
        public Task<IActionResult> ZoneSettingsContext(uint context)
        {
            return ZoneSettings(); // Always return the full list for now
        }
    }
}
