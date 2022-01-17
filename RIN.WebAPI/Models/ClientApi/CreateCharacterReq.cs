namespace RIN.WebAPI.Models.ClientApi
{
    public class CreateCharacterReq
    {
        public int    head             { get; set; }
        public string gender           { get; set; }
        public int    voice_set        { get; set; }
        public bool   is_dev           { get; set; }
        public int    start_class_id   { get; set; }
        public int    head_accessory_a { get; set; }
        public string environment      { get; set; }
        public int    skin_color_id    { get; set; }
        public string name             { get; set; }
        public int    eye_color_id     { get; set; }
        public int    hair_color_id    { get; set; }
    }
}