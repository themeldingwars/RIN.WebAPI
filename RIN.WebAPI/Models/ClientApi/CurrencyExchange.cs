using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi
{
    /// <summary>
    /// One rate the currency exchange offers, as the client's "Purchase Credits" tab reads it. The client counts in whole
    /// exchanges: daily_limit and remaining are how many exchanges of from_quantity into to_quantity are allowed.
    /// </summary>
    public class CurrencyExchangeRateResp
    {
        public int  from_sdb_id   { get; set; }
        public int  to_sdb_id     { get; set; }
        public int  from_quantity { get; set; }
        public int  to_quantity   { get; set; }
        public int  daily_limit   { get; set; }
        public int  remaining     { get; set; }
        public bool active        { get; set; }
    }

    /// <summary>
    /// quantity is what the character pays, in from_sdb_id. The client works it out in Lua, where every number is a
    /// double, so it is read as a decimal and has to be whole.
    /// </summary>
    public class CurrencyExchangeReq
    {
        [Required] public int     from_sdb_id { get; set; }
        [Required] public int     to_sdb_id   { get; set; }
        [Required] public decimal quantity    { get; set; }
    }

    // The client only reads remaining, the exchanges left today
    public class CurrencyExchangeResp
    {
        public int item_sdb_id { get; set; }
        public int quantity    { get; set; }
        public int remaining   { get; set; }
    }
}
