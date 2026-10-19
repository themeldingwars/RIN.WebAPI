using ProtoBuf;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    [ProtoInclude(1, typeof(SaveGameSessionData))]
    [ProtoInclude(2, typeof(SaveLgvRaceFinish))]
    [ProtoInclude(3, typeof(SaveCharacterUnlock))]
    // 4-13 are taken by the inventory PR's commands; keep numbering stable across the stack
    [ProtoInclude(14, typeof(DeleteCharacterUnlock))]
    public abstract class Command;

    [ProtoContract]
    public class SaveGameSessionData : Command
    {
        [ProtoMember(1)] public ulong CharacterId { get; set; }
        [ProtoMember(2)] public uint  ZoneId      { get; set; }
        [ProtoMember(3)] public uint  OutpostId   { get; set; }
        [ProtoMember(4)] public uint  TimePlayed  { get; set; }
    }

    [ProtoContract]
    public class SaveLgvRaceFinish : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public uint  LeaderboardId { get; set; }
        [ProtoMember(3)] public ulong TimeMs        { get; set; }
    }

    [ProtoContract]
    public class SaveCharacterUnlock : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public string UnlockType { get; set; } = string.Empty;
        [ProtoMember(3)] public uint UnlockId { get; set; }
        [ProtoMember(4)] public uint FrameId { get; set; }
    }

    [ProtoContract]
    public class DeleteCharacterUnlock : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public string UnlockType { get; set; } = string.Empty;
        [ProtoMember(3)] public uint UnlockId { get; set; }
        [ProtoMember(4)] public uint FrameId { get; set; }
    }
}
