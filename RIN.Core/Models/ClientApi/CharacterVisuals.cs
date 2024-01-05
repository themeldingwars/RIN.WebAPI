using System.Collections.Generic;
using ProtoBuf;
using RIN.Core.Common;
using RIN.Core.Models.ClientApi;

namespace RIN.Core.ClientApi
{
    [ProtoContract]
    public class CharacterBattleframeCombinedVisuals
    {
        [ProtoMember(1)]  public int                   id                { get; set; }
        [ProtoMember(2)]  public int                   race              { get; set; }
        [ProtoMember(3)]  public int                   gender            { get; set; }
        [ProtoMember(4)]  public WebIdValueColor       skin_color        { get; set; }
        [ProtoMember(5)]  public WebId                 voice_set         { get; set; }
        [ProtoMember(6)]  public WebId                 head              { get; set; }
        [ProtoMember(7)]  public WebIdValueColor       eye_color         { get; set; }
        [ProtoMember(8)]  public WebIdValueColor       lip_color         { get; set; }
        [ProtoMember(9)]  public WebIdValueColor       hair_color        { get; set; }
        [ProtoMember(10)] public WebIdValueColor       facial_hair_color { get; set; }
        [ProtoMember(11)] public List<WebIdValueColor> head_accessories  { get; set; }
        [ProtoMember(12)] public List<WebId>           ornaments         { get; set; }
        [ProtoMember(13)] public WebId                 eyes              { get; set; }
        [ProtoMember(14)] public WebIdValueColorId     hair              { get; set; }
        [ProtoMember(15)] public WebIdValueColorId     facial_hair       { get; set; }
        [ProtoMember(16)] public WebId                 glider            { get; set; }
        [ProtoMember(17)] public WebId                 vehicle           { get; set; }
        [ProtoMember(18)] public List<WebDecal>        decals            { get; set; }
        [ProtoMember(19)] public int                   warpaint_id       { get; set; }
        [ProtoMember(20)] public List<uint>            warpaint          { get; set; }
        [ProtoMember(21)] public List<int>             decalgradients    { get; set; }
        [ProtoMember(22)] public List<int>             warpaint_patterns { get; set; }
        [ProtoMember(23)] public List<int>             visual_overrides  { get; set; }
    }
}