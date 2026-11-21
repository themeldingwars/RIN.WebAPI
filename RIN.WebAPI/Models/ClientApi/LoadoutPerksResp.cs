namespace RIN.WebAPI.Models.ClientApi
{
    public class LoadoutPerksResp
    {
        public List<int> perks      { get; set; }
        public int       respecs    { get; set; }
        public int       max_points { get; set; }
    }
}
