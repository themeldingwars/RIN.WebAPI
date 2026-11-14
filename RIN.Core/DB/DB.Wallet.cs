using Dapper;
using Npgsql;

namespace RIN.Core.DB;

/// <summary>
/// A character's crystite and credits live in the wallet of the game server hosting it, which writes it over the
/// database, so RIN never changes them itself:
///  - a character pays through a wallet charge the game server settles. A payment that isn't kept comes back as a
///    delivery, so settling is safe to repeat.
///  - a character is paid through deliveries the game server claims, at its next login when it isn't online.
/// Red beans are the account's rb_balance, which only RIN keeps; a trigger announces every change of it.
/// </summary>
public partial class DB
{
    /// <summary>
    /// What a transaction changed that the game servers have to hear about once it commits.
    /// </summary>
    internal sealed class WalletChanges
    {
        private readonly HashSet<long> _deliveries = [];

        public async Task Deliver(NpgsqlConnection conn, NpgsqlTransaction tx, long characterGuid, int sdbId, long quantity, string reason)
        {
            const string DELIVER_SQL = @"
                    INSERT INTO webapi.""CharacterCurrencyDeliveries"" (character_guid, sdb_id, quantity, reason)
                    VALUES (@characterGuid, @sdbId, @quantity, @reason)";

            if (quantity <= 0)
                return;

            await conn.ExecuteAsync(DELIVER_SQL, new { characterGuid, sdbId, quantity = checked((int)quantity), reason }, tx);
            _deliveries.Add(characterGuid);
        }

        // Postgres holds notifications back until the commit, so nothing is announced that gets rolled back
        public async Task Notify(NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            const string DELIVERIES_SQL = @"
                    SELECT pg_notify('events', 'CurrencyDeliveriesAvailable->' || json_build_object('CharacterGuid', guid)::text)
                    FROM unnest(@characters) AS guid";

            if (_deliveries.Count > 0)
                await conn.ExecuteAsync(DELIVERIES_SQL, new { characters = _deliveries.ToArray() }, tx);
        }
    }

    // Marked claimed in the same statement, so two claims never both get it
    public async Task<List<CurrencyDeliveryRow>?> ClaimCurrencyDeliveries(long characterGuid)
    {
        const string CLAIM_SQL = @"
                WITH claimed AS (
                    UPDATE webapi.""CharacterCurrencyDeliveries""
                    SET claimed_at = now()
                    WHERE character_guid = @characterGuid
                    AND claimed_at IS NULL
                    RETURNING sdb_id, quantity
                )
                SELECT sdb_id, SUM(quantity)::integer AS quantity
                FROM claimed
                GROUP BY sdb_id";

        var failed = false;
        var claimed = await DBCall(async conn => (await conn.QueryAsync<CurrencyDeliveryRow>(CLAIM_SQL, new { characterGuid })).ToList(),
            _ => failed = true);

        return failed ? null : claimed;
    }

    public async Task<long?> GetRedBeanBalance(long characterGuid)
    {
        const string SELECT_SQL = @"
                SELECT a.rb_balance
                FROM webapi.""Characters"" c
                INNER JOIN webapi.""Accounts"" a ON a.account_id = c.account_id
                WHERE c.character_guid = @characterGuid";

        return await DBCall(conn => conn.QueryFirstOrDefaultAsync<long?>(SELECT_SQL, new { characterGuid }));
    }

    public async Task<bool> AwardRedBeans(long characterGuid, long amount)
    {
        const string AWARD_SQL = @"
                UPDATE webapi.""Accounts"" a
                SET rb_balance = a.rb_balance + @amount
                FROM webapi.""Characters"" c
                WHERE c.character_guid = @characterGuid
                AND a.account_id = c.account_id
                RETURNING a.account_id";

        if (amount <= 0)
            return false;

        return await DBCall(conn => conn.QueryFirstOrDefaultAsync<long?>(AWARD_SQL, new { characterGuid, amount })) != null;
    }

    // Only the payment that settles a pending charge can be kept, any other is delivered back. paymentId names the
    // game server's payment: settling it again gets the same answer, a character hosted twice pays under two ids.
    public Task<WalletChargeSettlement> SettleWalletCharge(uint kind, long id, long characterGuid, bool paid, Guid paymentId) => kind switch
    {
        WalletChargeKinds.CURRENCY_EXCHANGE  => SettleCurrencyExchange(id, characterGuid, paid, paymentId),
        WalletChargeKinds.RED_BEAN_BUY_ORDER => SettleRedBeanBuyOrder(id, characterGuid, paid, paymentId),
        _                                    => Task.FromResult(WalletChargeSettlement.Error)
    };

    private static async Task<WalletChargeSettlement?> FindWalletChargePayment(NpgsqlConnection conn, NpgsqlTransaction tx, uint kind, long chargeId,
                                                                              Guid paymentId)
    {
        const string SELECT_SQL = @"
                SELECT kept, status
                FROM webapi.""WalletChargePayments""
                WHERE kind = @kind
                AND charge_id = @chargeId
                AND payment_id = @paymentId";

        var row = await conn.QueryFirstOrDefaultAsync<WalletChargePaymentRow>(SELECT_SQL, new { kind = (short)kind, chargeId, paymentId }, tx);
        return row == null ? null : new WalletChargeSettlement(row.kept, row.status);
    }

    private static Task RecordWalletChargePayment(NpgsqlConnection conn, NpgsqlTransaction tx, uint kind, long chargeId, Guid paymentId,
                                                  long characterGuid, bool paid, WalletChargeSettlement settlement)
    {
        const string INSERT_SQL = @"
                INSERT INTO webapi.""WalletChargePayments"" (kind, charge_id, payment_id, character_guid, paid, kept, status)
                VALUES (@kind, @chargeId, @paymentId, @characterGuid, @paid, @kept, @status)";

        return conn.ExecuteAsync(INSERT_SQL, new
        {
            kind = (short)kind, chargeId, paymentId, characterGuid, paid, kept = settlement.Kept, status = (short)settlement.Status
        }, tx);
    }

    private sealed class WalletChargePaymentRow
    {
        public bool  kept   { get; set; }
        public short status { get; set; }
    }

    // Sent inside the transaction that opens the charge, so no game server sees a charge that isn't there
    private static Task RequestWalletCharge(NpgsqlConnection conn, NpgsqlTransaction tx, uint kind, long id, long characterGuid, int sdbId, long quantity)
    {
        const string NOTIFY_SQL = @"
                SELECT pg_notify('events', 'WalletChargeRequested->' || json_build_object(
                    'Kind', @kind,
                    'Id', @id,
                    'CharacterGuid', @characterGuid,
                    'SdbId', @sdbId,
                    'Quantity', @quantity)::text)";

        return conn.ExecuteAsync(NOTIFY_SQL, new { kind = (int)kind, id, characterGuid, sdbId, quantity }, tx);
    }
}
