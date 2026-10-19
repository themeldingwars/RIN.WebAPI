using ProtoBuf;
using RIN.Core.DB;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    [ProtoInclude(1, typeof(SaveGameSessionData))]
    [ProtoInclude(2, typeof(SaveLgvRaceFinish))]
    [ProtoInclude(3, typeof(SaveCharacterUnlock))]
    [ProtoInclude(4, typeof(SaveInventoryItem))]
    [ProtoInclude(5, typeof(SaveInventoryResource))]
    [ProtoInclude(6, typeof(SeedCharacterInventory))]
    [ProtoInclude(7, typeof(DeleteInventoryItem))]
    [ProtoInclude(8, typeof(SaveLoadoutSlot))]
    [ProtoInclude(9, typeof(SaveCharacterLoadout))]
    [ProtoInclude(10, typeof(SaveCurrentBattleframe))]
    [ProtoInclude(11, typeof(SaveItemModule))]
    [ProtoInclude(12, typeof(DeleteItemModule))]
    [ProtoInclude(13, typeof(DeleteLoadoutSlot))]
    [ProtoInclude(14, typeof(DeleteCharacterUnlock))]
    [ProtoInclude(15, typeof(SaveBattleframeXp))]
    [ProtoInclude(16, typeof(SaveBattleframeEliteXp))]
    [ProtoInclude(17, typeof(SaveBattleframeUpgrade))]
    [ProtoInclude(18, typeof(ClearBattleframeUpgrades))]
    [ProtoInclude(19, typeof(SaveBattleframeAvailableUpgrades))]
    [ProtoInclude(20, typeof(SeedCharacterFromTemplate))]
    [ProtoInclude(21, typeof(SaveLoadoutPerk))]
    [ProtoInclude(22, typeof(DeleteLoadoutPerk))]
    [ProtoInclude(23, typeof(SaveBattleframeBandwidth))]
    [ProtoInclude(24, typeof(SaveLoadoutVisuals))]
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

    [ProtoContract]
    public class SaveInventoryItem : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public long ItemGuid { get; set; }
        [ProtoMember(3)] public uint SdbId { get; set; }
        [ProtoMember(4)] public uint SubInventory { get; set; }
        [ProtoMember(5)] public uint DynamicFlags { get; set; }
        [ProtoMember(6)] public uint Durability { get; set; }
        [ProtoMember(7)] public List<ItemModule> Modules { get; set; } = new();
    }

    [ProtoContract]
    public class SaveInventoryResource : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public uint SdbId { get; set; }
        [ProtoMember(3)] public uint Quantity { get; set; }
        [ProtoMember(4)] public uint SubInventory { get; set; }
    }

    [ProtoContract]
    public class SeedCharacterInventory : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public List<InventoryItemData> Items { get; set; } = new();
        [ProtoMember(3)] public List<InventoryResourceData> Resources { get; set; } = new();
        [ProtoMember(4)] public List<InventoryLoadoutData> Loadouts { get; set; } = new();
        [ProtoMember(5)] public List<InventoryLoadoutSlotData> Slots { get; set; } = new();
        [ProtoMember(6)] public List<InventoryLoadoutVisualData> Visuals { get; set; } = new();
    }

    [ProtoContract]
    public class SeedCharacterFromTemplate : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public uint TemplateId { get; set; }
    }

    [ProtoContract]
    public class InventoryItemData
    {
        [ProtoMember(1)] public long ItemGuid { get; set; }
        [ProtoMember(2)] public uint SdbId { get; set; }
        [ProtoMember(3)] public uint SubInventory { get; set; }
        [ProtoMember(4)] public uint DynamicFlags { get; set; }
        [ProtoMember(5)] public uint Durability { get; set; }
        [ProtoMember(6)] public List<ItemModule> Modules { get; set; } = new();
    }

    [ProtoContract]
    public class InventoryResourceData
    {
        [ProtoMember(1)] public uint SdbId { get; set; }
        [ProtoMember(2)] public uint Quantity { get; set; }
        [ProtoMember(3)] public uint SubInventory { get; set; }
    }

    [ProtoContract]
    public class InventoryLoadoutData
    {
        [ProtoMember(1)] public int LoadoutId { get; set; }
        [ProtoMember(2)] public uint ChassisId { get; set; }
        [ProtoMember(3)] public string LoadoutName { get; set; } = string.Empty;
        [ProtoMember(4)] public string LoadoutType { get; set; } = string.Empty;
    }

    [ProtoContract]
    public class InventoryLoadoutSlotData
    {
        [ProtoMember(1)] public int LoadoutId { get; set; }
        [ProtoMember(2)] public uint ConfigId { get; set; }
        [ProtoMember(3)] public uint SlotIndex { get; set; }
        [ProtoMember(4)] public long ItemGuid { get; set; }
    }

    [ProtoContract]
    public class LoadoutVisual
    {
        [ProtoMember(1)] public uint VisualType { get; set; }
        [ProtoMember(2)] public uint ItemSdbId { get; set; }
        [ProtoMember(3)] public uint Data1 { get; set; }
        [ProtoMember(4)] public uint Data2 { get; set; }

        // Pattern: 4 floats (rotation in degrees, scale, horizontal offset, vertical offset).
        // Decal: 12 floats (row major 3x3 rotation and scale, then translation in metres). Empty otherwise.
        [ProtoMember(5)] public List<float> Transform { get; set; } = new();
    }

    [ProtoContract]
    public class InventoryLoadoutVisualData
    {
        [ProtoMember(1)] public int LoadoutId { get; set; }
        [ProtoMember(2)] public uint ConfigId { get; set; }
        [ProtoMember(3)] public List<LoadoutVisual> Visuals { get; set; } = new();
    }

    [ProtoContract]
    public class DeleteInventoryItem : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public long ItemGuid { get; set; }
    }

    [ProtoContract]
    public class SaveLoadoutSlot : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public int LoadoutId { get; set; }
        [ProtoMember(3)] public uint ConfigId { get; set; }
        [ProtoMember(4)] public uint SlotIndex { get; set; }
        [ProtoMember(5)] public long ItemGuid { get; set; }
    }

    [ProtoContract]
    public class DeleteLoadoutSlot : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public int LoadoutId { get; set; }
        [ProtoMember(3)] public uint ConfigId { get; set; }
        [ProtoMember(4)] public uint SlotIndex { get; set; }
    }

    [ProtoContract]
    public class SaveCharacterLoadout : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public int LoadoutId { get; set; }
        [ProtoMember(3)] public int ChassisId { get; set; }
        [ProtoMember(4)] public PlayerBattleframeVisuals Visuals { get; set; }
        [ProtoMember(5)] public List<CharacterLoadoutSlotEntry> SlottedItems { get; set; } = new();
    }

    [ProtoContract]
    public class SaveCurrentBattleframe : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public int ChassisId { get; set; }
    }

    [ProtoContract]
    public class SaveItemModule : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public long ItemGuid { get; set; }
        [ProtoMember(3)] public uint SlotIndex { get; set; }
        [ProtoMember(4)] public uint ModuleSdbId { get; set; }
    }

    [ProtoContract]
    public class DeleteItemModule : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public long ItemGuid { get; set; }
        [ProtoMember(3)] public uint SlotIndex { get; set; }
    }

    [ProtoContract]
    public class SaveBattleframeXp : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public uint BattleframeSdbId { get; set; }
        [ProtoMember(3)] public uint Level { get; set; }
        [ProtoMember(4)] public ulong CurrentXp { get; set; }
        [ProtoMember(5)] public ulong LifetimeXp { get; set; }
    }

    [ProtoContract]
    public class SaveBattleframeEliteXp : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public uint BattleframeSdbId { get; set; }
        [ProtoMember(3)] public uint EliteLevel { get; set; }
        [ProtoMember(4)] public ulong CurrentEliteXp { get; set; }
        [ProtoMember(5)] public ulong LifetimeEliteXp { get; set; }
        [ProtoMember(6)] public uint ElitePoints { get; set; }
    }

    [ProtoContract]
    public class SaveBattleframeUpgrade : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public uint BattleframeSdbId { get; set; }
        [ProtoMember(3)] public uint UpgradeId { get; set; }
        [ProtoMember(4)] public uint Count { get; set; }
    }

    [ProtoContract]
    public class ClearBattleframeUpgrades : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public uint BattleframeSdbId { get; set; }
    }

    [ProtoContract]
    public class SaveBattleframeAvailableUpgrades : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public uint BattleframeSdbId { get; set; }
        [ProtoMember(3)] public List<BattleframeAvailableUpgradeEntry> Upgrades { get; set; } = new();
    }

    [ProtoContract]
    public class SaveLoadoutPerk : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public int LoadoutId { get; set; }
        [ProtoMember(3)] public uint PerkItemSdbId { get; set; }
        [ProtoMember(4)] public uint Level { get; set; }
    }

    [ProtoContract]
    public class DeleteLoadoutPerk : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public int LoadoutId { get; set; }
        [ProtoMember(3)] public uint PerkItemSdbId { get; set; }
    }

    [ProtoContract]
    public class SaveBattleframeBandwidth : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public uint BattleframeSdbId { get; set; }
        [ProtoMember(3)] public uint Bandwidth { get; set; }
    }

    [ProtoContract]
    public class SaveLoadoutVisuals : Command
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
        [ProtoMember(2)] public int LoadoutId { get; set; }

        // 0 PvE, 1 PvP - the same numbering SaveLoadoutSlot uses.
        [ProtoMember(3)] public uint ConfigId { get; set; }

        [ProtoMember(4)] public List<LoadoutVisual> Visuals { get; set; } = new();
    }
}
