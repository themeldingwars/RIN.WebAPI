using ProtoBuf;

namespace RIN.WebAPI.Models.Common
{
    [ProtoContract]
    public class GearSlot
    {
        [ProtoMember(1)] public int   slot_type_id { get; set; }
        [ProtoMember(2)] public int   sdb_id       { get; set; }
        [ProtoMember(3)] public ulong item_guid    { get; set; }
    }
}