namespace RIN.WebAPI.Models.ClientApi
{
    public class LoginEvents
    {
        public long count => results.Count;
        public List<LoginEvent> results { get; set; } = new List<LoginEvent>();
    }
}
