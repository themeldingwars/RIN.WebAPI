namespace RIN.Core.Models.ClientApi
{
    public class ZoneCertRequirements
    {
        public uint   id                 { get; set; }
        public uint   zone_setting_id    { get; set; }
        public string presence           { get; set; } = string.Empty;
        public uint   cert_id            { get; set; }
        public string authorize_position { get; set; } = string.Empty;
        public string difficulty_key     { get; set; } = string.Empty;

        public uint certificate_id
        {
            set => id = value;
        }
    }
}
