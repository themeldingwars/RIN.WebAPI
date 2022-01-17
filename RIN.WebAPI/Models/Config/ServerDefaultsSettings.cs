namespace RIN.WebAPI.Models.Config
{
    public class ServerDefaultsSettings
    {
        public const string SERVER_DEFAULTS = "ServerDefaults";
            
        public int CharaterLimitPerAccount { get; set; } = 2;
        public int CharaterNameMaxLength   { get; set; } = 40;
        public int CharaterNameMinLength   { get; set; } = 1;
    }
}