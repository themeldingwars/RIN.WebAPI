using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIN.Core.Common
{
    public class GarageSlot
    {
        public int id { get; set; }
        public string name { get; set; }
        public long character_guid { get; set; }
        public string garage_type { get; set; }
        public long item_guid { get; set; }
        public List<EquipmentSlot> equipped_slots { get; set; }
        public SlotLimits limits { get; set; }
        public List<Decal> decals { get; set; }
        public int visual_loadout_id { get; set; }
        public int warpaint_id { get; set; }
        public List<WarpaintPattern> warpaintpatterns { get; set; }
        public List<VisualOverride> visual_overrides { get; set; }
        public bool unlocked { get; set; }
        public int expires_in_secs { get; set; }
    }

    public class SlotLimits
    {
        public int abilities { get; set; }
    }

    public class EquipmentSlot
    {
        public int slot_type_id { get; set; }
        public int sdb_id { get; set; }
        public long item_guid { get; set; }
        public int allocated_power { get; set; }
        public bool unlocked { get; set; }
    }

    public class WarpaintPattern
    {
        public int sdb_id { get; set; }
        public float[] transform { get; set; }
        public int usage { get; set; }
    }

    public class Decal
    {
        public int sdb_id { get; set; }
        public uint color { get; set; }
        public float[] transform { get; set; }
    }

    public class VisualOverride
    {
        public int slot_type_id { get; set; }
        public int visual_id { get; set; }
    }
}
