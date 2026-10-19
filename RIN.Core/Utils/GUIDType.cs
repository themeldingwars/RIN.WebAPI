namespace RIN.Core
{
    public enum GUIDType : byte
    {
        Character = 0xFE,
        V1        = 0x7F,
        Transient = 0xFF, // Map Entitys
        Items     = 0xFD, // also used for battleframes
        Army      = 0xFC,
        Zone      = 0xFB  // world/zone entities, not items
    }
}
