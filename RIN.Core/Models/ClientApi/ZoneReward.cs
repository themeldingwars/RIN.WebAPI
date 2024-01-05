namespace RIN.Core.Models
{
    public class ZoneReward
    {
        public List<long> items { get; set; } = new();
        public List<long> loot_sdb_ids { get; set; } = new();
        public List<long> loots { get; set; } = new();
    }
}
