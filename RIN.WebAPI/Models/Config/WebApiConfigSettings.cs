namespace RIN.WebAPI.Models
{
    public class WebApiConfigSettings
    {
        public const string NAME = "WebApiConfig";

        public string BaseURL          { get; set; }      = string.Empty;
        public bool   VerifyRed5Sig    { get; set; }      = true;
        public TimeSpan UserSessionLifetime { get; set; } = TimeSpan.FromMinutes(30);
    }
}