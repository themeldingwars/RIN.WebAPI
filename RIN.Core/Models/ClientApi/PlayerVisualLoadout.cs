using Newtonsoft.Json;
using RIN.Core.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIN.Core.Models
{
    public class PlayerVisualLoadout
    {
        public long id { get; set; }
        public long character_guid { get; set; }
        public int gender { get; set; }
        public int race { get; set; }
        public int skin_color_id { get; set; }
        public int voice_set_id { get; set; }
        public int head_id { get; set; }
        public int eye_id { get; set; }
        public int eye_color_id { get; set; }
        public int lip_color_id { get; set; }
        public int hair_color_id { get; set; }
        public int facial_hair_color_id { get; set; }
        public List<RemoteItem> head_accessories { get; set; } = new ();
        public List<RemoteItem> ornaments { get; set; } = new ();

        // Did you know that hair is a head accessory
        [JsonIgnore] public int hair_id        => head_accessories.Count >= 1 ? head_accessories[0].remote_id : 0;
        [JsonIgnore] public int facial_hair_id => head_accessories.Count > 1 ? head_accessories[1].remote_id : 0;
    }

    public class RemoteItem
    {
        public int remote_id { get; set; }
    }
}
