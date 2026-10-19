using System.Text.Json.Serialization;

namespace RIN.Core.DB
{
    public class LoadoutVisualEntry
    {
        // LoadoutConfig_Visual.LoadoutVisualType on the game wire.
        public const uint TypePalette = 9;
        public const uint TypePattern = 10;
        public const uint TypeDecal   = 11;
        public const uint TypeGlider  = 13;
        public const uint TypeVehicle = 14;

        // dbvisualrecords::WarpaintPalette.id, dbvisualrecords::CziPattern.id, dbvisualrecords::TattooDecal.id,
        // and for glider and vehicle dbitems::RootItem.sdb_id. Pattern and decal ids are not RootItem ids.
        [JsonPropertyName("visualType")] public uint VisualType { get; set; }
        [JsonPropertyName("itemSdbId")]   public uint ItemSdbId  { get; set; }

        // Pattern only: the chassis region, 1 armor, 2 bodysuit. 0 for every other type.
        [JsonPropertyName("data1")] public uint Data1 { get; set; }

        // Decal only: the colour as RGBA. 0 for every other type.
        [JsonPropertyName("data2")] public uint Data2 { get; set; }

        // Pattern: exactly 4 floats - rotation in degrees, scale, horizontal offset, vertical offset.
        // Decal: exactly 12 - a row major 3x3 rotation and scale, then a 3 float translation in metres.
        // Empty for every other type.
        [JsonPropertyName("transform")] public List<float> Transform { get; set; } = new();
    }
}
