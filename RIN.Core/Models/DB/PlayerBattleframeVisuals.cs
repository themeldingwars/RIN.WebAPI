using System.Collections.Generic;
using ProtoBuf;
using RIN.Core.ClientApi;
using RIN.Core.Common;
using RIN.Core.Models.ClientApi;

namespace RIN.Core.DB
{
    [ProtoContract]
    public class PlayerBattleframeVisuals
    {
        [ProtoMember(1)] public List<WebDecal>  decals            { get; set; } = new();
        [ProtoMember(2)] public int             warpaint_id       { get; set; }
        [ProtoMember(3)] public List<uint>      warpaint          { get; set; }
        [ProtoMember(4)] public List<int>       decalgradients    { get; set; }
        [ProtoMember(5)] public List<int>       warpaint_patterns { get; set; }
        [ProtoMember(6)] public List<int>       visual_overrides  { get; set; }
        
        public void ApplyToCharacterVisuals(CharacterBattleframeCombinedVisuals cVisuals)
        {
            cVisuals.decals            = decals;
            cVisuals.warpaint_id       = warpaint_id;
            cVisuals.warpaint          = warpaint;
            cVisuals.decalgradients    = decalgradients;
            cVisuals.warpaint_patterns = warpaint_patterns;
            cVisuals.visual_overrides  = visual_overrides;
        }
        
        public static PlayerBattleframeVisuals CreateDefault()
        {
            var visuals = new PlayerBattleframeVisuals
            {
                decals            = new List<WebDecal>(),
                warpaint_id       = 1033,
                warpaint          = new List<uint> {4294910212, 2631073792, 830865408, 1246298112, 2494725038, 3430953281, 3430953281},
                decalgradients    = new List<int>(),
                warpaint_patterns = new List<int>(),
                visual_overrides  = new List<int>()
            };

            return visuals;
        }
    }
}