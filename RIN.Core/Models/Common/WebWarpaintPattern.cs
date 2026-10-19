using ProtoBuf;

namespace RIN.Core.Common
{
    [ProtoContract]
    public class WebWarpaintPattern
    {
        // dbvisualrecords::CziPattern.id
        [ProtoMember(1)] public int sdb_id { get; set; }

        // Always 0: the client carries no per-pattern colour, only the decal block has one.
        [ProtoMember(2)] public int color { get; set; }

        [ProtoMember(3)] public List<int> transform { get; set; } = new();

        // Chassis region: 0 armor, 1 bodysuit.
        [ProtoMember(4)] public int usage { get; set; }
    }
}
