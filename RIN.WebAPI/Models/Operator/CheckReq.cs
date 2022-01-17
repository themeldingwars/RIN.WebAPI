namespace RIN.WebAPI.Models.Operator
{
    public class CheckReq
    {
        public string environment { get; set; }
        public int    build       { get; set; }
    }
}