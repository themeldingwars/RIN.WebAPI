namespace RIN.WebAPI.Models.Config
{
    public class DevServerSettings
    {
        public const string NAME = "DevServer";
        
        public bool   EnableLocalDev   { get; set; } = true; // if true use the set game server
        public string DevGameServerURL { get; set; } = string.Empty;
        public string DevTicket        { get; set; } = string.Empty;
        public string DevSessionId     { get; set; } = string.Empty;
    }
}