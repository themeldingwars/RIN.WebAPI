namespace RIN.WebAPI.Models.Operator
{
    public class Hosts
    {
        public string frontend_host     { get; set; }
        public string store_host        { get; set; }
        public string chat_server       { get; set; }
        public string replay_host       { get; set; }
        public string web_host          { get; set; }
        public string market_host       { get; set; }
        public string ingame_host       { get; set; }
        public string clientapi_host    { get; set; }
        public string web_asset_host    { get; set; }
        public string web_accounts_host { get; set; }
        public string rhsigscan_host    { get; set; }
    }
}