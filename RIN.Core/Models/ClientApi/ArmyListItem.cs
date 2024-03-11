namespace RIN.Core.ClientApi
{
    public class ArmyListItem
    {
        public long   army_guid     { get; set; }
        public string name          { get; set; }
        public string personality   { get; set; }
        public bool   is_recruiting { get; set; }
        public string region        { get; set; }
        public uint   member_count  { get; set; }
    }
}
