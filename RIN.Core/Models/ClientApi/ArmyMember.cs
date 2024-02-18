namespace RIN.Core.ClientApi
{
    public class ArmyMember
    {
        public long character_guid { get; set; }
        public long army_rank_id { get; set; }
        public int rank_position { get; set; }
        public string rank_name { get; set; }
        public long last_seen_at { get; set; }
        public int last_zone_id { get; set; }
        public bool is_online { get; set; }
        public string? public_note { get; set; }
        public string? officer_note { get; set; }
        public string name { get; set; }
        // public uint? current_level { get; set; }
        // public uint? current_frame_sdb_id { get; set; }
    }
}