using ProtoBuf;

namespace RIN.Core.Models
{
    [ProtoContract]
    public class BasicCharacterInfo
    {
        [ProtoMember(1)] public string Name { get; set; }
        [ProtoMember(2)] public byte Race { get; set; }
        [ProtoMember(3)] public byte Gender { get; set; }
        [ProtoMember(4)] public int TitleId { get; set; }
        [ProtoMember(5)] public long CurrentBattleframeId { get; set; }
        [ProtoMember(6)] public uint CurrentBattleframeSDBId { get; set; }
        [ProtoMember(7)] public string ArmyTag { get; set; }
        [ProtoMember(8)] public ulong ArmyGuid { get; set; }
        [ProtoMember(9)] public bool ArmyIsOfficer { get; set; }
    }
}
