namespace RIN.WebAPI.Models
{
    public class WebApiConfigSettings
    {
        public const string WEB_API_CONFIG = "WebApiConfig";

        public string BaseURL         { get; set; } = string.Empty;
        public bool   VerifyRed5Sig   { get; set; } = true;
        public string DBConnectionStr { get; set; } = string.Empty;
    }
}