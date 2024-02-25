namespace RIN.WebAPI.Models.ClientApi
{
    public class PageResults<T>
    {
        public string  page        { get; set; }
        public long    total_count { get; set; }
        public List<T> results     { get; set; }
    }
}