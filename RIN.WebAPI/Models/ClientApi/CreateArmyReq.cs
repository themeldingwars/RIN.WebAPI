namespace RIN.WebAPI.Models.ClientApi
{
    public class CreateArmyReq
    {
        public string name { get; set; }
        public string website { get; set; }
        public string description { get; set; }
        public bool is_recruiting { get; set; }
        public string playstyle { get; set; }
        public string region { get; set; }
        public string personality { get; set; }
    }
}