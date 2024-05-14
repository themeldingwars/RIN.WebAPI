namespace RIN.Core.Models.ClientApi
{
    public class ZoneDifficultyLevels
    {
        public uint   id                  { get; set; }
        public uint   zone_setting_id     { get; set; }
        public string ui_string           { get; set; } = string.Empty;
        public uint   min_level           { get; set; }
        public string difficulty_key      { get; set; } = string.Empty;
        public uint   min_players         { get; set; }
        public uint   min_players_accept  { get; set; }
        public uint   max_players         { get; set; }
        public uint   group_min_players   { get; set; }
        public uint   group_max_players   { get; set; }
        public uint   display_level       { get; set; }
        public uint   max_suggested_level { get; set; }
    }
}
