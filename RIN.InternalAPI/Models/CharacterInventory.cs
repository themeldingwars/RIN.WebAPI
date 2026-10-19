using ProtoBuf;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    public class CharacterInventoryResponse
    {
        [ProtoMember(1)] public List<CharacterItem> Items { get; set; } = new();
        [ProtoMember(2)] public List<CharacterResource> Resources { get; set; } = new();
        [ProtoMember(3)] public List<CharacterLoadout> Loadouts { get; set; } = new();
        [ProtoMember(4)] public List<CharacterLoadoutSlotEntry> Slots { get; set; } = new();
        [ProtoMember(5)] public List<CharacterLoadoutPerkEntry> LoadoutPerks { get; set; } = new();
    }

    [ProtoContract]
    public class CharacterItem
    {
        [ProtoMember(1)] public ulong Guid { get; set; }
        [ProtoMember(2)] public uint SdbId { get; set; }
        [ProtoMember(3)] public uint SubInventory { get; set; }
        [ProtoMember(4)] public uint DynamicFlags { get; set; }
        [ProtoMember(5)] public uint Durability { get; set; }
        [ProtoMember(6)] public List<ItemModule> Modules { get; set; } = new();
    }

    [ProtoContract]
    public class ItemModule
    {
        [ProtoMember(1)] public uint SlotIndex { get; set; }
        [ProtoMember(2)] public uint ModuleSdbId { get; set; }
    }

    [ProtoContract]
    public class CharacterResource
    {
        [ProtoMember(1)] public uint SdbId { get; set; }
        [ProtoMember(2)] public uint Quantity { get; set; }
        [ProtoMember(3)] public uint SubInventory { get; set; }
    }

    [ProtoContract]
    public class CharacterLoadout
    {
        [ProtoMember(1)] public int LoadoutId { get; set; }
        [ProtoMember(2)] public int ChassisId { get; set; }
        [ProtoMember(3)] public string LoadoutName { get; set; } = string.Empty;
        [ProtoMember(4)] public string LoadoutType { get; set; } = string.Empty;
    }

    [ProtoContract]
    public class CharacterLoadoutPerkEntry
    {
        [ProtoMember(1)] public int LoadoutId { get; set; }
        [ProtoMember(2)] public uint PerkItemSdbId { get; set; }
        [ProtoMember(3)] public uint Level { get; set; }
    }

    [ProtoContract]
    public class CharacterLoadoutSlotEntry
    {
        [ProtoMember(1)] public int LoadoutId { get; set; }
        [ProtoMember(2)] public uint ConfigId { get; set; }
        [ProtoMember(3)] public uint SlotIndex { get; set; }
        [ProtoMember(4)] public long ItemGuid { get; set; }
    }
}
