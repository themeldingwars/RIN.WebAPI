using ProtoBuf;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    public class SettleWalletChargeReq
    {
        [ProtoMember(1)] public uint  Kind          { get; set; }
        [ProtoMember(2)] public long  Id            { get; set; }
        [ProtoMember(3)] public ulong CharacterGuid { get; set; }
        [ProtoMember(4)] public bool  Paid          { get; set; }
        [ProtoMember(5)] public string PaymentId    { get; set; } = string.Empty;
    }

    // Unless Kept, RIN delivers back whatever the game server took. Status -1 means RIN couldn't settle it.
    [ProtoContract]
    public class SettleWalletChargeResp
    {
        [ProtoMember(1)] public bool Kept   { get; set; }
        [ProtoMember(2)] public int  Status { get; set; }
    }

    [ProtoContract]
    public class CurrencyDelivery
    {
        [ProtoMember(1)] public uint SdbId    { get; set; }
        [ProtoMember(2)] public uint Quantity { get; set; }
    }

    [ProtoContract]
    public class CurrencyDeliveriesResponse
    {
        [ProtoMember(1)] public List<CurrencyDelivery> Deliveries { get; set; } = new();
    }

    [ProtoContract]
    public class RedBeanBalanceResponse
    {
        [ProtoMember(1)] public bool Found   { get; set; }
        [ProtoMember(2)] public long Balance { get; set; }
    }
}
