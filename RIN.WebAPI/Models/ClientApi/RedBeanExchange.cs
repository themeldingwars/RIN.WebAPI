using System.ComponentModel.DataAnnotations;
using RIN.Core.DB;

namespace RIN.WebAPI.Models.ClientApi
{
    /// <summary>
    /// A new order, as the client's Red Bean Exchange tab posts it. The client works the numbers out in Lua, where every
    /// number is a double, so they are read as decimals and have to be whole.
    /// </summary>
    public class RedBeanOrderReq
    {
        [Required] public string  operation      { get; set; } = string.Empty;  // "buy" or "sell"
        [Required] public decimal rb             { get; set; }
        [Required] public decimal price_per_unit { get; set; }
    }

    public class RedBeanBookLevel
    {
        public long rb             { get; set; }
        public int  price_per_unit { get; set; }
    }

    /// <summary>
    /// The book as the client lists it: the dearest buy offers and the cheapest sell listings first.
    /// </summary>
    public class RedBeanBookResp
    {
        public List<RedBeanBookLevel> buy  { get; set; } = [];
        public List<RedBeanBookLevel> sell { get; set; } = [];
    }

    /// <summary>
    /// An order, for the client's active listings and personal history. The client shows rb - rb_remaining as traded,
    /// looks status up in its CURRENCY_EXCHANGE_STATE_ texts and passes created_at, unix seconds, to
    /// System.GetElapsedUnixTime.
    /// </summary>
    public class RedBeanOrderResp
    {
        public long   id             { get; set; }
        public string operation      { get; set; } = string.Empty;
        public int    rb             { get; set; }
        public int    rb_remaining   { get; set; }
        public int    price_per_unit { get; set; }
        public int    status         { get; set; }
        public long   created_at     { get; set; }
        public long   updated_at     { get; set; }

        public static RedBeanOrderResp From(RedBeanExchangeOrderRow order) => new()
        {
            id             = order.id,
            operation      = order.operation,
            rb             = order.rb,
            rb_remaining   = order.rb_remaining,
            price_per_unit = order.price_per_unit,
            status         = order.status,
            created_at     = ToUnixSeconds(order.created_at),
            updated_at     = ToUnixSeconds(order.updated_at)
        };

        private static long ToUnixSeconds(DateTime time) => new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Utc)).ToUnixTimeSeconds();
    }
}
