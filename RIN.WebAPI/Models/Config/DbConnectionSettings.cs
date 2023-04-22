namespace RIN.WebAPI.Models.Config
{
    public class DbConnectionSettings
    {
        public const string NAME = "DBConnConfig";

        public string DBConnStr { get; set; }    = string.Empty;
        public string SDBConnStr { get; set; }   = string.Empty;
        public bool LogDbCallTimes { get; set; } = false;
    }
}
