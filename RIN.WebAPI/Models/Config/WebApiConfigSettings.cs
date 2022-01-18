namespace RIN.WebAPI.Models
{
    public class WebApiConfigSettings
    {
        public const string NAME = "WebApiConfig";

        public string BaseURL          { get; set; } = string.Empty;
        public bool   VerifyRed5Sig    { get; set; } = true;
        public string DBConnectionStr  { get; set; } = string.Empty;
        public string SDBConnectionStr { get; set; } = string.Empty;
        public bool   LogDbCallTimes   { get; set; } = false;
    }
}