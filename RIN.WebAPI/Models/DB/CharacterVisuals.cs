using System.Collections.Generic;
using ProtoBuf;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Models.Common;

namespace RIN.WebAPI.Models.DB
{
    [ProtoContract]
    public class CharacterVisuals
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
        
        public void ApplyToCharacterVisuals(CharacterBattleframeCombinedVisuals cVisuals)
        {
            //cVisuals.id                = id;
            cVisuals.race              = race;
            cVisuals.gender            = gender;
            cVisuals.skin_color        = skin_color;
            cVisuals.voice_set         = voice_set;
            cVisuals.head              = head;
            cVisuals.eye_color         = eye_color;
            cVisuals.lip_color         = lip_color;
            cVisuals.hair_color        = hair_color;
            cVisuals.facial_hair_color = facial_hair_color;
            cVisuals.head_accessories  = head_accessories;
            cVisuals.ornaments         = ornaments;
            cVisuals.eyes              = eyes;
            cVisuals.hair              = hair;
            cVisuals.facial_hair       = facial_hair;
            cVisuals.glider            = glider;
            cVisuals.vehicle           = vehicle;
        }
    }
}