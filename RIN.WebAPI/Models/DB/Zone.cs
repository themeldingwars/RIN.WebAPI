using RIN.WebAPI.Models.ClientApi;

namespace RIN.WebAPI.Models.DB
{
    public class DBZoneSettings
    {
        public uint id { get; set; }
        public uint zone_id { get; set; }
        public string mission_id { get; set; }
        public string gametype { get; set; }
        public string instance_type_pool { get; set; }
        public bool is_preview_zone { get; set; }
        public string displayed_name { get; set; }
        public string displayed_desc { get; set; }
        public string description { get; set; }
        public string displayed_gametype { get; set; }
        public bool cert_required { get; set; }
        public float xp_bonus { get; set; }
        public string sort_order { get; set; }
        public uint rotation_priority { get; set; }
        public bool skip_matchmaking { get; set; }
        public bool queueing_enabled { get; set; }
        public uint team_count { get; set; }
        public uint min_players_per_team { get; set; }
        public uint max_players_per_team { get; set; }
        public uint min_players_accept_per_team { get; set; }
        public bool challenge_enabled { get; set; }
        public uint challenge_min_players_per_team { get; set; }
        public uint challenge_max_players_per_team { get; set; }
        public ZoneImages images { get; set; }
    }

    public class DBZoneCertRequirements
    {
        public uint id { get; set; }
        public uint cert_id { get; set; }
        // public uint zone_settings_id { get; set; }
        public string authorize_position { get; set; }
        public string difficulty_key { get; set; }
        public string presence { get; set; }        
    }

    public class DBZoneDifficultyLevels
    {
        public uint id { get; set; }
        // public uint zone_settings_id { get; set; }
        public string difficulty_key { get; set; }
        public string ui_string { get; set; }
        public uint display_level { get; set; }
        public uint min_level { get; set; }
        public uint max_suggested_level { get; set; }
        public uint min_players { get; set; }
        public uint max_players { get; set; }
        public uint min_players_accept { get; set; }
        public uint group_min_players { get; set; }
        public uint group_max_players { get; set; }
    }
}
