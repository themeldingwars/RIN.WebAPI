using System.Collections.Generic;
using RIN.WebAPI.Models.Common;

namespace RIN.WebAPI.Models.ClientApi
{
    public class CharacterVisuals
    {
        public int                   id                { get; set; }
        public int                   race              { get; set; }
        public int                   gender            { get; set; }
        public WebIdValueColor       skin_color        { get; set; }
        public WebId                 voice_set         { get; set; }
        public WebId                 head              { get; set; }
        public WebIdValueColor       eye_color         { get; set; }
        public WebIdValueColor       lip_color         { get; set; }
        public WebIdValueColor       hair_color        { get; set; }
        public WebIdValueColor       facial_hair_color { get; set; }
        public List<WebIdValueColor> head_accessories  { get; set; }
        public List<WebId>           ornaments         { get; set; }
        public WebId                 eyes              { get; set; }
        public WebIdValueColorId     hair              { get; set; }
        public WebIdValueColorId     facial_hair       { get; set; }
        public WebId                 glider            { get; set; }
        public WebId                 vehicle           { get; set; }
        public List<WebId>           decals            { get; set; }
        public int                   warpaint_id       { get; set; }
        public List<uint>            warpaint          { get; set; }
        public List<int>             decalgradients    { get; set; }
        public List<int>             warpaint_patterns { get; set; }
        public List<int>             visual_overrides  { get; set; }
    }
}