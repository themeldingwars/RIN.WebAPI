using RIN.WebAPI.Models.Operator;

namespace RIN.WebAPI.Models.ClientApi
{
    public class OracleTicket
    {
        public string country           { get; set; }
        public string datacenter        { get; set; }
        public string hostname          { get; set; }
        public string matrix_url        { get; set; }
        public Hosts  operator_override { get; set; }
        public string session_id        { get; set; }
        public string ticket            { get; set; }
    }
}