using ProtoBuf;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    public class BattleframeProgressionResponse
    {
        [ProtoMember(1)] public List<BattleframeProgressionEntry> Progression { get; set; } = new();
    }

    [ProtoContract]
    public class BattleframeProgressionEntry
    {
        [ProtoMember(1)] public ulong BattleframeSdbId { get; set; }
        [ProtoMember(2)] public ulong BattleframeGuid { get; set; }
        [ProtoMember(3)] public uint Level { get; set; }
        [ProtoMember(4)] public ulong CurrentXp { get; set; }
        [ProtoMember(5)] public ulong LifetimeXp { get; set; }
        [ProtoMember(6)] public uint EliteLevel { get; set; }
        [ProtoMember(7)] public ulong CurrentEliteXp { get; set; }
        [ProtoMember(8)] public ulong LifetimeEliteXp { get; set; }
        [ProtoMember(9)] public uint ElitePoints { get; set; }
        [ProtoMember(10)] public List<BattleframeUpgradeEntry> PreviousUpgrades { get; set; } = new();
        [ProtoMember(11)] public List<BattleframeAvailableUpgradeEntry> AvailableUpgrades { get; set; } = new();
    }

    [ProtoContract]
    public class BattleframeUpgradeEntry
    {
        [ProtoMember(1)] public uint UpgradeId { get; set; }
        [ProtoMember(2)] public uint Count { get; set; }
    }

    [ProtoContract]
    public class BattleframeAvailableUpgradeEntry
    {
        [ProtoMember(1)] public uint UpgradeIndex { get; set; }
        [ProtoMember(2)] public uint UpgradeId { get; set; }
        [ProtoMember(3)] public uint Category { get; set; }
        [ProtoMember(4)] public float StatValue { get; set; }
        [ProtoMember(5)] public uint? Count { get; set; }
        [ProtoMember(6)] public uint? ItemSdbId { get; set; }
        [ProtoMember(7)] public uint LocalizationId { get; set; }
    }
}
