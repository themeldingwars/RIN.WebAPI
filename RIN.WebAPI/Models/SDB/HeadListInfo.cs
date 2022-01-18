namespace RIN.WebAPI.Models.SDB
{
    public class HeadListInfo
    {
        public int    head_id           { get; set; }
        public string sex               { get; set; }
        public byte   race_id           { get; set; }
        public byte   player_selectable { get; set; }
        public string lang_name         { get; set; }
    }
}