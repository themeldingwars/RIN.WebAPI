using ProtoBuf;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    [ProtoInclude(1, typeof(ArmyApplicationApproved))]
    [ProtoInclude(2, typeof(ArmyApplicationReceived))]
    [ProtoInclude(3, typeof(ArmyApplicationRejected))]
    [ProtoInclude(4, typeof(ArmyApplicationsUpdated))]
    [ProtoInclude(5, typeof(ArmyIdChanged))]
    [ProtoInclude(6, typeof(ArmyInfoUpdated))]
    [ProtoInclude(7, typeof(ArmyInviteApproved))]
    [ProtoInclude(8, typeof(ArmyInviteReceived))]
    [ProtoInclude(9, typeof(ArmyInviteRejected))]
    [ProtoInclude(10, typeof(ArmyMembersUpdated))]
    [ProtoInclude(11, typeof(ArmyRanksUpdated))]
    [ProtoInclude(12, typeof(ArmyTagUpdated))]
    [ProtoInclude(13, typeof(CharacterVisualsUpdated))]
    [ProtoInclude(14, typeof(InventoryUpdated))]
    [ProtoInclude(15, typeof(UnlocksUpdated))]
    [ProtoInclude(16, typeof(FriendNotification))]
    [ProtoInclude(17, typeof(LoadoutPerksUpdated))]
    [ProtoInclude(18, typeof(WalletChargeRequested))]
    [ProtoInclude(19, typeof(CurrencyDeliveriesAvailable))]
    [ProtoInclude(20, typeof(RedBeansUpdated))]
    public abstract class Event;

    [ProtoContract]
    public class ArmyApplicationApproved : Event
    {
        [ProtoMember(1)] public ulong  CharacterGuid { get; set; }
        [ProtoMember(2)] public string InitiatorName { get; set; }
    }

    [ProtoContract]
    public class ArmyApplicationReceived : Event
    {
        [ProtoMember(1)] public ulong[] ArmyMemberGuids { get; set; }
        [ProtoMember(2)] public string  InitiatorName   { get; set; }
    }

    [ProtoContract]
    public class ArmyApplicationRejected : Event
    {
        [ProtoMember(1)] public ulong  CharacterGuid { get; set; }
        [ProtoMember(2)] public string InitiatorName { get; set; }
    }

    [ProtoContract]
    public class ArmyApplicationsUpdated : Event
    {
        [ProtoMember(1)] public ulong[] ArmyMemberGuids { get; set; }
    }

    [ProtoContract]
    public class ArmyIdChanged : Event
    {
        [ProtoMember(1)] public ulong  ArmyGuid      { get; set; }
        [ProtoMember(2)] public ulong  CharacterGuid { get; set; }
        [ProtoMember(3)] public bool   IsOfficer     { get; set; }
        [ProtoMember(4)] public string ArmyTag       { get; set; }
    }

    [ProtoContract]
    public class ArmyInfoUpdated : Event
    {
        [ProtoMember(1)] public ulong ArmyGuid { get; set; }
    }

    [ProtoContract]
    public class ArmyInviteApproved : Event
    {
        [ProtoMember(1)] public ulong  ArmyGuid      { get; set; }
        [ProtoMember(2)] public string InitiatorName { get; set; }
    }

    [ProtoContract]
    public class ArmyInviteReceived : Event
    {
        [ProtoMember(1)] public ulong  ArmyGuid      { get; set; }
        [ProtoMember(2)] public string ArmyName      { get; set; }
        [ProtoMember(3)] public ulong  CharacterGuid { get; set; }
        [ProtoMember(4)] public ulong  Id            { get; set; }
        [ProtoMember(5)] public string Message       { get; set; }
        [ProtoMember(6)] public string InitiatorName { get; set; }
    }

    [ProtoContract]
    public class ArmyInviteRejected : Event
    {
        [ProtoMember(1)] public ulong[] ArmyMemberGuids { get; set; }
        [ProtoMember(2)] public string  InitiatorName   { get; set; }
    }

    [ProtoContract]
    public class ArmyMembersUpdated : Event
    {
        [ProtoMember(1)] public ulong ArmyGuid { get; set; }
    }

    [ProtoContract]
    public class ArmyRanksUpdated : Event
    {
        [ProtoMember(1)] public ulong ArmyGuid { get; set; }
    }

    [ProtoContract]
    public class ArmyTagUpdated : Event
    {
        [ProtoMember(1)] public ulong  ArmyGuid { get; set; }
        [ProtoMember(2)] public string ArmyTag  { get; set; }
    }

    [ProtoContract]
    public class CharacterVisualsUpdated : Event
    {
        [ProtoMember(1)] public ulong                          CharacterGuid                  { get; set; }
        [ProtoMember(2)] public CharacterAndBattleframeVisuals CharacterAndBattleframeVisuals { get; set; }
    }

    [ProtoContract]
    public class InventoryUpdated : Event
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
    }

    [ProtoContract]
    public class UnlocksUpdated : Event
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
    }

    // The game server hosting the character takes the payment and settles it with SettleWalletCharge; the others ignore it
    [ProtoContract]
    public class WalletChargeRequested : Event
    {
        [ProtoMember(1)] public uint  Kind          { get; set; }
        [ProtoMember(2)] public long  Id            { get; set; }
        [ProtoMember(3)] public ulong CharacterGuid { get; set; }
        [ProtoMember(4)] public uint  SdbId         { get; set; }
        [ProtoMember(5)] public uint  Quantity      { get; set; }
    }

    [ProtoContract]
    public class CurrencyDeliveriesAvailable : Event
    {
        [ProtoMember(1)] public ulong CharacterGuid { get; set; }
    }

    [ProtoContract]
    public class RedBeansUpdated : Event
    {
        [ProtoMember(1)] public long AccountId { get; set; }
        [ProtoMember(2)] public long Balance   { get; set; }
    }

    [ProtoContract]
    public class LoadoutPerksUpdated : Event
    {
        [ProtoMember(1)] public ulong  CharacterGuid { get; set; }
        [ProtoMember(2)] public int    LoadoutId     { get; set; }
        [ProtoMember(3)] public uint[] PerkSdbIds    { get; set; }
    }
}
