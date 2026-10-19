using ProtoBuf;

namespace RIN.Core.Models.DB
{
    [ProtoContract]
    public class CharacterUnlockEntry
    {
        [ProtoMember(1)] public string UnlockType { get; set; } = string.Empty;
        [ProtoMember(2)] public uint UnlockId { get; set; }
        [ProtoMember(3)] public uint FrameId { get; set; }
    }
}
