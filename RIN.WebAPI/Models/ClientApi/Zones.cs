namespace RIN.WebAPI.Models.ClientApi
{
    public class Zones
    {
        public string matrix_url { get; set; } = string.Empty;
        public string match { get; set; } = string.Empty;
        public string revision { get; set; } = string.Empty;
        public string protocol_version { get; set; } = string.Empty;
        public string zone_name { get; set; } = string.Empty;
        public string owner { get; set; } = string.Empty;
        public long players { get; set; }
    }
}
