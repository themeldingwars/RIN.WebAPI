namespace RIN.Core
{
    [Flags]
    public enum ItemDynamicFlags : byte
    {
        None       = 0x00,
        IsBound    = 0x01,
        Unk_0x02   = 0x02, // is_new?
        IsEquipped = 0x04  // derived from CharacterLoadoutSlots, ignored on write
    }
}
