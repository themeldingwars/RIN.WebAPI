using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIN.Core.Common
{
    public struct TradePrice
    {
        public int amount { get; set; }
        public int currency_remote_id { get; set; }
        public string currency_type { get; set; }
        public int id { get; set; }
    }
}
