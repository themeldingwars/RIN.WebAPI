using System.Collections.Generic;

namespace RIN.WebAPI.Models.ClientApi
{
    public class ValidateNameResp
    {
        public string       code    { get; set; }
        public string       message { get; set; }
        public string       name    { get; set; }
        public List<string> reason  { get; set; }
        public bool         valid   { get; set; }
    }
}