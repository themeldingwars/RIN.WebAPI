using Dapper;
using Newtonsoft.Json;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Models.DB;

namespace RIN.WebAPI.DB
{
    public partial class DB
    {
        public async Task<List<ZoneSettingsResp>> GetZoneSettings()
        {
            const string SELECT_SQL = @"SELECT 
                    id,
                    zone_id,
                    mission_id,
                    gametype,
                    instance_type_pool,
                    is_preview_zone,
                    displayed_name,
                    displayed_desc,
                    description,
                    displayed_gametype,
                    cert_required,
                    xp_bonus,
                    sort_order,
                    rotation_priority,
                    skip_matchmaking,
                    queueing_enabled,
                    team_count,
                    min_players_per_team,
                    max_players_per_team,
                    min_players_accept_per_team,
                    challenge_enabled,
                    challenge_min_players_per_team,
                    challenge_max_players_per_team,
                    images
                FROM webapi.""ZoneSettings""
                WHERE is_active = True";

            var zone_settings = await DBCall(async conn => conn.Query<ZoneSettings>(SELECT_SQL));

            var zones = new List<ZoneSettingsResp>(zone_settings.Count());
            foreach (var setting in zone_settings)
            {
                var images = JsonConvert.DeserializeObject<ZoneImages>(setting.images);

                var zoneSettings = new ZoneSettingsResp
                {
                    id = setting.id,
                    zone_id = setting.zone_id,
                    mission_id = setting.mission_id,
                    gametype = setting.gametype,
                    instance_type_pool = setting.instance_type_pool,
                    is_preview_zone = setting.is_preview_zone,
                    displayed_name = setting.displayed_name,
                    displayed_desc = setting.displayed_desc,
                    description = setting.description,
                    displayed_gametype = setting.displayed_gametype,
                    cert_required = setting.cert_required,
                    xp_bonus = setting.xp_bonus,
                    sort_order = setting.sort_order,
                    rotation_priority = setting.rotation_priority,
                    skip_matchmaking = setting.skip_matchmaking,
                    queueing_enabled = setting.queueing_enabled,
                    team_count = setting.team_count,
                    min_players_per_team = setting.min_players_per_team,
                    max_players_per_team = setting.max_players_per_team,
                    min_players_accept_per_team = setting.min_players_accept_per_team,
                    challenge_enabled = setting.challenge_enabled,
                    challenge_min_players_per_team = setting.challenge_min_players_per_team,
                    challenge_max_players_per_team = setting.challenge_max_players_per_team,
                    images = images
                };

                zones.Add(zoneSettings);
            }

            return zones;
        }

        public async Task<List<ZoneCertRequirements>> GetZoneCertRequirements(uint zone_setting_id)
        {
            const string SELECT_SQL_CERT = @"SELECT 
                    id,
                    zone_setting_id,
                    cert_id,
                    authorize_position,
                    difficulty_key,
                    presence
                FROM webapi.""ZoneCertificates""
                WHERE zone_setting_id = @zone_setting_id";

            var certificates = (List<ZoneCertRequirements>)await DBCall(async conn => conn.Query<ZoneCertRequirements>(SELECT_SQL_CERT));

            return certificates;
        }

        public async Task<List<ZoneDifficultyLevels>> GetZoneDifficultyLevels(uint zone_setting_id)
        {
            const string SELECT_SQL_DIFFICULTY = @"SELECT 
                    id,
                    zone_setting_id,
                    difficulty_key,
                    ui_string,
                    display_level,
                    min_level,
                    max_suggested_level,
                    min_players,
                    max_players,
                    min_players_accept,
                    group_min_players,
                    group_max_players
                FROM webapi.""ZoneDifficulty""
                WHERE zone_setting_id = @zone_setting_id";

            var difficulty = (List<ZoneDifficultyLevels>)await DBCall(async conn => conn.Query<ZoneDifficultyLevels>(SELECT_SQL_DIFFICULTY));

            return difficulty;
        }
    }
}