using ProtoBuf;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    [ProtoInclude(1, typeof(SaveGameSessionData))]
    [ProtoInclude(2, typeof(SaveLgvRaceFinish))]
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
}
