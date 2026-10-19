using System.Text.Json;
using RIN.Core.ClientApi;
using RIN.Core.Common;
using RIN.Core.DB;
using RIN.Core.Models.ClientApi;
using RIN.Core.SDB;

namespace RIN.Core.Utils
{
    /// <summary>
    /// The battleframe half of the web API's visuals block, worked out from one loadout config's stored visual
    /// set. The web API shape is not the stored shape: patterns and decals carry differently encoded transforms,
    /// and the seven warpaint colours are not stored at all - only the palette ids are.
    /// </summary>
    public class BattleframeVisualsBlock
    {
        public int                      warpaint_id       { get; set; }
        public List<uint>               warpaint          { get; set; } = new();
        public List<WebWarpaintPattern> warpaint_patterns { get; set; } = new();
        public List<WebDecal>           decals            { get; set; } = new();
        public List<int>                decalgradients    { get; set; } = new();
        public List<int>                visual_overrides  { get; set; } = new();

        public int glider_id  { get; set; }
        public int vehicle_id { get; set; }

        public void ApplyTo(CharacterBattleframeCombinedVisuals cVisuals)
        {
            cVisuals.warpaint_id       = warpaint_id;
            cVisuals.warpaint          = warpaint;
            cVisuals.warpaint_patterns = warpaint_patterns;
            cVisuals.decals            = decals;
            cVisuals.decalgradients    = decalgradients;
            cVisuals.visual_overrides  = visual_overrides;

            // The per-loadout entry wins over the character level field
            if (glider_id != 0)
                cVisuals.glider = new WebId(glider_id);
            if (vehicle_id != 0)
                cVisuals.vehicle = new WebId(vehicle_id);
        }
    }

    /// <summary>
    /// Reading and writing a loadout config's visual set.
    /// </summary>
    public static class LoadoutVisuals
    {
        // A pattern's four dials: rotation in degrees, scale, horizontal offset, vertical offset.
        public const int PatternTransformValues = 4;

        // A decal's 3x3 rotation and scale, row major, followed by a 3 float translation in metres.
        public const int DecalTransformValues = 12;

        // The web API and the game wire both hold a pattern's scale and offsets as 16 bit fixed point with
        // twelve fractional bits, and its rotation as degrees times 90.
        public const int WireFixedPoint        = 4096;
        public const int WireMaxValue          = ushort.MaxValue;
        public const int WireAngleUnitsPerDegree = 90;
        public const int WireAngleFullTurn     = 360 * WireAngleUnitsPerDegree;

        // Unspecified usage
        public const int NoRegionUsage = 255;

        // What a frame's colours are before any palette paints anything.
        private const uint BaseColor = 0xFF000000;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            // Hand-edited rows are a debugging tool, so accept a key written any way.
            PropertyNameCaseInsensitive = true
        };

        public static string ToJson(IEnumerable<LoadoutVisualEntry> visuals, out int droppedEntries)
        {
            var kept = new List<LoadoutVisualEntry>();

            droppedEntries = 0;

            foreach (var visual in visuals ?? new List<LoadoutVisualEntry>())
            {
                if (visual == null || HasNonFiniteTransform(visual))
                {
                    droppedEntries++;
                    continue;
                }

                kept.Add(visual);
            }

            return JsonSerializer.Serialize(kept, JsonOptions);
        }

        private static bool HasNonFiniteTransform(LoadoutVisualEntry visual)
        {
            if (visual.Transform == null)
                return false;

            foreach (var value in visual.Transform)
                if (float.IsNaN(value) || float.IsInfinity(value))
                    return true;

            return false;
        }

        public static List<LoadoutVisualEntry> FromJson(string json)
        {
            var set = new List<LoadoutVisualEntry>();

            if (string.IsNullOrWhiteSpace(json))
                return set;

            using var doc = ParseJsonArray(json);
            if (doc == null)
                return set;

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                try
                {
                    var entry = element.Deserialize<LoadoutVisualEntry>(JsonOptions);
                    if (entry != null)
                        set.Add(entry);
                }
                catch (JsonException)
                {
                }
            }

            return set;
        }

        private static JsonDocument ParseJsonArray(string json)
        {
            try
            {
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    doc.Dispose();
                    return null;
                }

                return doc;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Scale or an offset as the fixed point number the visuals block carries.
        /// </summary>
        /// <param name="value">The value as a real number</param>
        /// <returns>The value times 4096, held to what one ushort can carry</returns>
        public static int ToWireValue(float value)
        {
            if (float.IsNaN(value) || value <= 0f)
                return 0;

            // Round rather than truncate: the values arrive as floats, so a truncating cast can land one unit
            // short of the number the player dialled in.
            return (int) Math.Min(Math.Round(value * WireFixedPoint), WireMaxValue);
        }

        /// <summary>
        /// A rotation as the angle the visuals block carries.
        /// </summary>
        /// <param name="degrees">The rotation in degrees, which may be negative or past one turn</param>
        /// <returns>The degrees times 90, wrapped into one turn</returns>
        public static int ToWireAngle(float degrees)
        {
            if (float.IsNaN(degrees))
                return 0;

            var units = (long) Math.Round(degrees * WireAngleUnitsPerDegree);
            units %= WireAngleFullTurn;
            if (units < 0)
                units += WireAngleFullTurn;

            return (int) units;
        }

        /// <summary>
        /// The chassis region a pattern was painted on, as the web API counts it.
        ///
        /// The client numbers the regions from one (1 armor, 2 bodysuit, 3 weapon), the web API and the game
        /// wire from zero. Anything outside those three is reported as <see cref="NoRegionUsage"/>.
        /// </summary>
        /// <param name="data1">The region the client sent</param>
        /// <returns>The region the visuals block counts, or 255 when there is none</returns>
        public static int PatternUsage(uint data1) => data1 is >= 1 and <= 3 ? (int) data1 - 1 : NoRegionUsage;

        /// <summary>
        /// The four dials of a pattern as the web API's transform array: the same fixed point the game wire carries.
        /// </summary>
        public static List<int> PatternToWire(List<float> transform)
        {
            var values = new List<int>(PatternTransformValues);

            for (var index = 0; index < PatternTransformValues; index++)
            {
                var value = transform != null && index < transform.Count ? transform[index] : 0f;
                values.Add(index == 0 ? ToWireAngle(value) : ToWireValue(value));
            }

            return values;
        }

        /// <summary>
        /// Work out the web API's battleframe visuals from one config's stored set.
        /// </summary>
        /// <param name="visuals">The stored set, in stored order</param>
        /// <param name="palettes">sdb WarpaintPalette rows by id, for every palette the set or the frame defaults name</param>
        /// <param name="chassisDefaults">the frame's default palettes, or null if sdb has no row for it</param>
        public static BattleframeVisualsBlock Build(IReadOnlyList<LoadoutVisualEntry> visuals,
                                                    IReadOnlyDictionary<long, WarpaintPaletteColors> palettes,
                                                    BattleframePaletteDefaults? chassisDefaults)
        {
            var block = new BattleframeVisualsBlock();

            uint fullbody  = 0;
            uint armor     = 0;
            uint bodysuit  = 0;
            uint glow      = 0;
            uint lastPalette = 0;

            foreach (var visual in visuals ?? Array.Empty<LoadoutVisualEntry>())
            {
                switch (visual.VisualType)
                {
                    case LoadoutVisualEntry.TypePalette:
                        lastPalette = visual.ItemSdbId;

                        // A fullbody palette paints all seven colours, so it takes every region slot.
                        if (HasRegion(visual.ItemSdbId, palettes, WarpaintPaletteColors.FullbodyRegion))
                        {
                            fullbody = armor = bodysuit = glow = visual.ItemSdbId;
                            break;
                        }

                        if (HasRegion(visual.ItemSdbId, palettes, WarpaintPaletteColors.ArmorRegion))
                            armor = visual.ItemSdbId;
                        if (HasRegion(visual.ItemSdbId, palettes, WarpaintPaletteColors.BodysuitRegion))
                            bodysuit = visual.ItemSdbId;
                        if (HasRegion(visual.ItemSdbId, palettes, WarpaintPaletteColors.GlowRegion))
                            glow = visual.ItemSdbId;
                        break;

                    case LoadoutVisualEntry.TypePattern:
                        block.warpaint_patterns.Add(new WebWarpaintPattern
                        {
                            sdb_id    = (int) visual.ItemSdbId,
                            color     = 0,   // the client carries no per-pattern color
                            transform = PatternToWire(visual.Transform),
                            usage     = PatternUsage(visual.Data1)
                        });
                        break;

                    case LoadoutVisualEntry.TypeDecal:
                        block.decals.Add(new WebDecal
                        {
                            sdb_id    = (int) visual.ItemSdbId,
                            color     = visual.Data2,
                            transform = visual.Transform?.ToArray() ?? Array.Empty<float>()
                        });
                        break;

                    case LoadoutVisualEntry.TypeGlider:
                        block.glider_id = (int) visual.ItemSdbId;
                        break;

                    case LoadoutVisualEntry.TypeVehicle:
                        block.vehicle_id = (int) visual.ItemSdbId;
                        break;
                }
            }

            block.warpaint_id = (int) (fullbody != 0 ? fullbody : lastPalette);

            block.warpaint = BlendColors(fullbody, armor, bodysuit, glow, palettes, chassisDefaults);

            return block;
        }

        private static bool HasRegion(uint paletteId, IReadOnlyDictionary<long, WarpaintPaletteColors> palettes, uint region)
        {
            return palettes != null
                   && palettes.TryGetValue(paletteId, out var palette)
                   && (palette.type_flags & region) != 0;
        }

        /// <summary>
        /// The frame's seven colours: the dbitems::Battleframe defaults with the player's picks replacing the
        /// regions they painted. Order is armor 1-3, bodysuit 1-2, glow 1-2.
        /// </summary>
        private static List<uint> BlendColors(uint fullbody, uint armor, uint bodysuit, uint glow,
                                              IReadOnlyDictionary<long, WarpaintPaletteColors> palettes,
                                              BattleframePaletteDefaults? chassisDefaults)
        {
            var colors = new List<uint>(7) { BaseColor, BaseColor, BaseColor, BaseColor, BaseColor, BaseColor, BaseColor };

            // A region the player did not paint belongs to the frame.
            var fullbodyId  = fullbody  != 0 ? fullbody  : chassisDefaults?.default_fullbody_palette_id  ?? 0;
            var armorId     = armor     != 0 ? armor     : chassisDefaults?.default_armor_palette_id     ?? 0;
            var bodysuitId  = bodysuit  != 0 ? bodysuit  : chassisDefaults?.default_bodysuit_palette_id  ?? 0;
            var glowId      = glow      != 0 ? glow      : chassisDefaults?.default_glow_palette_id      ?? 0;

            foreach (var paletteId in new[] { fullbodyId, armorId, bodysuitId, glowId })
            {
                if (paletteId == 0 || palettes == null || !palettes.TryGetValue(paletteId, out var palette))
                    continue;

                var paletteColors = palette.PackColors();

                if ((palette.type_flags & WarpaintPaletteColors.FullbodyRegion) != 0)
                    for (var index = 0; index < 7; index++)
                        colors[index] = paletteColors[index];

                if ((palette.type_flags & WarpaintPaletteColors.ArmorRegion) != 0)
                    for (var index = 0; index < 3; index++)
                        colors[index] = paletteColors[index];

                if ((palette.type_flags & WarpaintPaletteColors.BodysuitRegion) != 0)
                    for (var index = 3; index < 5; index++)
                        colors[index] = paletteColors[index];

                if ((palette.type_flags & WarpaintPaletteColors.GlowRegion) != 0)
                    for (var index = 5; index < 7; index++)
                        colors[index] = paletteColors[index];
            }

            return colors;
        }
    }
}
