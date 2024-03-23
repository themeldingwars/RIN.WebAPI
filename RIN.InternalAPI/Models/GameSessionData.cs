using ProtoBuf;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    public class GameSessionData
    {
        [ProtoMember(1)] public ulong CharacterId { get; set; }
        [ProtoMember(2)] public uint  ZoneId      { get; set; }
        [ProtoMember(3)] public uint  OutpostId   { get; set; }
        [ProtoMember(4)] public uint  TimePlayed  { get; set; }
    }
}
