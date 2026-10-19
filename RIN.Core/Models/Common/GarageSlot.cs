namespace RIN.Core.Common
{
    public class GarageSlot
    {
        public long item_guid { get; set; }
        public int id { get; set; }
        public List<EquipmentSlot> equipped_slots { get; set; } = [];
    }

    public class EquipmentSlot
    {
        public int slot_type_id { get; set; }
        public int sdb_id { get; set; }
        public long item_guid { get; set; }
        public int allocated_power { get; set; }
        public bool unlocked { get; set; }
    }
}
