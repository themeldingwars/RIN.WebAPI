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
            var zones = await Db.GetZoneSettings();

            var zoneSettings = new List<ZoneSettings>(zones.Count());
            foreach (var zone in zones)
            {
                var certs = Db.GetZoneCertRequirements(zone.zone_id);
                var diffLevels = Db.GetZoneDifficultyLevels(zone.zone_id);
                await Task.WhenAll(certs, diffLevels);

                zone.cert_requirements = certs.Result;
                zone.difficulty_levels = diffLevels.Result;

                zoneSettings.Add(zone);
            }

            return zoneSettings;
        }
    }
}