using ProtoBuf;

namespace RIN.Core.Models.ClientApi
{
    [ProtoContract]
    public class WebDecal
    {
        [ProtoMember(1)] public int sdb_id { get; set; }
        [ProtoMember(2)] public int color { get; set; }
        [ProtoMember(3)] public float[] transform { get; set; } 
    }
}
