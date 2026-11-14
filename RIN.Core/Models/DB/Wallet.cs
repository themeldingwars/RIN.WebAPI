namespace RIN.Core.DB
{
    // The game server echoes the kind back when it settles, so RIN knows which table the charge's id is in
    public static class WalletChargeKinds
    {
        public const uint CURRENCY_EXCHANGE  = 1;  // CharacterCurrencyExchanges.id, the crystite of a purchase
        public const uint RED_BEAN_BUY_ORDER = 2;  // RedBeanExchangeOrders.id, the credits a buy order holds
    }

    // Unless Kept, anything the game server took is delivered back. Status is the charge's row status, for the log.
    public readonly record struct WalletChargeSettlement(bool Kept, int Status)
    {
        public static readonly WalletChargeSettlement Error = new(false, -1);
    }

    public sealed class CurrencyDeliveryRow
    {
        public int sdb_id   { get; set; }
        public int quantity { get; set; }
    }
}
