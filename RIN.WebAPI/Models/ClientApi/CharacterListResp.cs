using RIN.Core.ClientApi;
using System.Collections.Generic;

namespace RIN.WebAPI.Models.ClientApi
{
    public class CharacterListResp
    {
        public List<Character> characters       { get; set; }
        public bool            is_dev           { get; set; }
        public long            rb_balance       { get; set; }
        public int             name_change_cost { get; set; }
    }
}