namespace RIN.Core.Common
{
    public class TradeItem
    {
        public int duration { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public IEnumerable<TradePrice> prices { get; set; }
        public int quantity { get; set; }
        public int remote_id { get; set; }
        public string remote_type { get; set; }
        public string unlock_context { get; set; }
    }
}
