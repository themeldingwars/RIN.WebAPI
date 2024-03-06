namespace RIN.Core.ClientApi
{
    public class ArmyRank
    {
        public long      id             { get; set; }
        public long      army_guid      { get; set; }
        public string    name           { get; set; }
        public bool      is_commander   { get; set; }
        public bool      can_invite     { get; set; }
        public bool      can_kick       { get; set; }
        public DateTime  created_at     { get; set; }
        public DateTime? updated_at     { get; set; }
        public bool      can_edit       { get; set; }
        public bool      can_promote    { get; set; }
        public uint      position       { get; set; }
        public bool      is_officer     { get; set; }
        public bool      can_mass_email { get; set; }
        public bool      is_default     { get; set; }
    }
}
