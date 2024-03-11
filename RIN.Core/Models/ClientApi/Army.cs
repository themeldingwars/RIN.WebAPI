using RIN.Core.Models.ClientApi;

namespace RIN.Core.ClientApi
{
    public class Army
    {
        public long              army_guid      { get; set; }
        public long              established_at { get; set; }
        public bool              is_recruiting  { get; set; }
        public string            motd           { get; set; }
        public string            name           { get; set; }
        public string?           tag            { get; set; }
        public string            playstyle      { get; set; }
        public string            description    { get; set; }
        public string            personality    { get; set; }
        public string            website        { get; set; }
        public string            login_message  { get; set; }
        public string            region         { get; set; }
        public List<ArmyOfficer> officers       { get; set; }
        public uint              member_count   { get; set; }
    }
}
