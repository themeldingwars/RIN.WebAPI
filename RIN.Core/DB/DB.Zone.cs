using Dapper;
using Newtonsoft.Json;
using RIN.Core.Models.ClientApi;

namespace RIN.Core.DB
{
    public partial class DB
    {
        public async Task<List<ZoneSettings>> GetZoneSettings()
        {
            const string SELECT_SQL = @"
                SELECT
                   zs.id,
                   zs.zone_id,
                   zs.mission_id,
                   zs.gametype,
                   zs.instance_type_pool,
                   zs.is_preview_zone,
                   zs.displayed_name,
                   zs.displayed_desc,
                   zs.description,
                   zs.displayed_gametype,
                   zs.cert_required,
                   zs.xp_bonus,
                   zs.sort_order,
                   zs.rotation_priority,
                   zs.skip_matchmaking,
                   zs.queueing_enabled,
                   zs.team_count,
                   zs.min_players_per_team,
                   zs.max_players_per_team,
                   zs.min_players_accept_per_team,
                   zs.challenge_enabled,
                   zs.challenge_min_players_per_team,
                   zs.challenge_max_players_per_team,
                   zs.images AS images_json,
           
                   zc.id as certificate_id,
                   zc.zone_setting_id,
                   zc.cert_id,
                   zc.authorize_position,
                   zc.difficulty_key,
                   zc.presence,
                    
                   zd.id as difficulty_id,
                   zd.zone_setting_id,
                   zd.difficulty_key,
                   zd.ui_string,
                   zd.display_level,
                   zd.min_level,
                   zd.max_suggested_level,
                   zd.min_players,
                   zd.max_players,
                   zd.min_players_accept,
                   zd.group_min_players,
                   zd.group_max_players
                FROM webapi.""ZoneSettings"" zs
                LEFT JOIN webapi.""ZoneCertificates"" zc ON zs.id = zc.zone_setting_id
                LEFT JOIN webapi.""ZoneDifficulty"" zd ON zs.id = zd.zone_setting_id
                WHERE zs.is_active = True
                ORDER BY zs.id";

            var zoneSettingsDict = new Dictionary<uint, ZoneSettings>();
            var certRequirementsIds = new HashSet<uint>();
            var difficultyLevelsIds = new HashSet<uint>();

            await DBCall(async conn =>
            {
                return await conn.QueryAsync<ZoneSettings, string, ZoneCertRequirements?, ZoneDifficultyLevels?, ZoneSettings>(
                    SELECT_SQL,
                    (zoneSettings, zoneImages, zoneCertRequirements, zoneDifficultyLevels) =>
                    {
                        if (!zoneSettingsDict.TryGetValue(zoneSettings.id, out var zoneEntry))
                        {
                            zoneEntry = zoneSettings;
                            zoneEntry.cert_requirements = [];
                            zoneEntry.difficulty_levels = [];
                            zoneSettingsDict.Add(zoneEntry.id, zoneEntry);
                        }

                        zoneEntry.images = JsonConvert.DeserializeObject<ZoneImages>(zoneImages);

                        if (zoneCertRequirements != null && !certRequirementsIds.Contains(zoneCertRequirements.id))
                        {
                           zoneEntry.cert_requirements.Add(zoneCertRequirements);
                           certRequirementsIds.Add(zoneCertRequirements.id);
                        }

                        if (zoneDifficultyLevels != null && !difficultyLevelsIds.Contains(zoneDifficultyLevels.id))
                        {
                            zoneEntry.difficulty_levels.Add(zoneDifficultyLevels);
                            difficultyLevelsIds.Add(zoneDifficultyLevels.id);
                        }

                        return zoneEntry;
                    },
                    splitOn: "images_json,certificate_id,difficulty_id");
            });

            return zoneSettingsDict.Values.ToList();
        }
    }
}
