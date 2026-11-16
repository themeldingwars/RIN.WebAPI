namespace RIN.Core.DB
{
    /// <summary>
    /// DailyLimit is in ToSdbId per UTC day. The client works in whole exchanges, so a purchase is a multiple of both
    /// quantities.
    /// </summary>
    public sealed record CurrencyExchangeRate(int FromSdbId, int ToSdbId, int FromQuantity, int ToQuantity, int DailyLimit)
    {
        public int RemainingExchanges(int boughtToday) => Math.Max(0, DailyLimit - boughtToday) / ToQuantity;
    }

    public static class CurrencyExchangeRates
    {
        public const int CRYSTITE_SDB_ID = 10;
        public const int CREDITS_SDB_ID  = 30101;
    }

    // Error is never stored, it means the database could not be reached or the request is not there
    public enum CurrencyExchangeStatus
    {
        Error        = -1,
        Pending      = 0,
        Exchanged    = 1,
        NotEnough    = 2,
        LimitReached = 3,
        NoAnswer     = 4   // no game server took it on, the character is offline or between zones
    }
}
