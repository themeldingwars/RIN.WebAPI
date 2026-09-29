namespace RIN.WebAPI.Models.InGame
{
    public class SocialStaticData
    {
        public List<SocialZone> zones { get; set; } = [];
    }

    public class SocialZone
    {
        public int    zone_id { get; set; }
        public string title   { get; set; } = string.Empty;
    }
}
