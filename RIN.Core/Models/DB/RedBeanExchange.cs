namespace RIN.Core.DB
{
    // The client checks the price bounds too, from its own copy of them
    public static class RedBeanExchangeRules
    {
        public const string BUY  = "buy";
        public const string SELL = "sell";

        public const int MIN_PRICE_PER_UNIT = 100;
        public const int MAX_PRICE_PER_UNIT = 500;

        // Keeps what an order holds inside an int: 100000 red beans at 500 credits is 50 million credits
        public const int MAX_RB_PER_ORDER = 100000;

        // The client lists 8 of a character's open orders, so it can always see, and cancel, all of them
        public const int MAX_OPEN_ORDERS = 8;

        // The client shows the top 10 price levels of each side of the book
        public const int BOOK_DEPTH = 10;

        // The client asks for the 50 most recent finished orders
        public const int HISTORY_LENGTH = 50;
    }

    // The client's numbering; it knows more states than the exchange uses
    public static class RedBeanOrderStatus
    {
        public const short PENDING   = 0;  // a buy order waiting for the game server to take the credits
        public const short ACTIVE    = 2;
        public const short COMPLETED = 3;
        public const short FAILED    = 4;  // a buy order whose credits couldn't be taken
        public const short CANCELLED = 5;
    }

    // Error means the database could not be reached
    public enum RedBeanOrderOutcome
    {
        Error,
        Placed,         // the order is on the book, or already traded in full
        Pending,
        Cancelled,
        NotEnough,      // the account doesn't have the red beans, or the character the credits
        TooManyOrders,
        NoAnswer,       // no game server took the credits of a buy order in time
        NotFound,
        NotActive       // the order can't be cancelled, it is finished or still waits for its credits
    }

    public sealed class RedBeanExchangeOrderRow
    {
        public long     id              { get; set; }
        public long     character_guid  { get; set; }
        public long     account_id      { get; set; }
        public string   operation       { get; set; } = string.Empty;
        public int      rb              { get; set; }
        public int      rb_remaining    { get; set; }
        public int      price_per_unit  { get; set; }
        public short    status          { get; set; }
        public DateTime created_at      { get; set; }
        public DateTime updated_at      { get; set; }

        public bool IsBuy => operation == RedBeanExchangeRules.BUY;
    }

    public sealed class RedBeanBookLevelRow
    {
        public string operation      { get; set; } = string.Empty;
        public int    price_per_unit { get; set; }
        public long   rb             { get; set; }
    }
}
