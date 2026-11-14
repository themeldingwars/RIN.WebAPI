using Dapper;

namespace RIN.Core.DB;

// A purchase is a wallet charge (see DB.Wallet)
public partial class DB
{
    // Charges nobody settled by now are given up on, whatever became of the web request that opened them
    private const int WALLET_CHARGE_STALE_SECONDS = 30;

    // Settling takes this lock per character. The two key form keeps it apart from the single key locks friends use.
    private const string CURRENCY_EXCHANGE_LOCK_SQL = @"SELECT pg_advisory_xact_lock(4501, hashtext(@characterGuid::text))";

    // The exchange's day is the UTC one
    private const string BOUGHT_TODAY_SQL = @"
            SELECT COALESCE(SUM(to_quantity), 0)::integer
            FROM webapi.""CharacterCurrencyExchanges""
            WHERE character_guid = @characterGuid
            AND exchange_day = (now() AT TIME ZONE 'UTC')::date
            AND status = 1
            AND from_sdb_id = @FromSdbId
            AND to_sdb_id = @ToSdbId";

    public async Task<int?> GetCurrencyBoughtToday(long characterGuid, CurrencyExchangeRate rate)
    {
        var failed = false;
        var bought = await DBCall(conn => conn.QueryFirstAsync<int>(BOUGHT_TODAY_SQL, new { characterGuid, rate.FromSdbId, rate.ToSdbId }),
            _ => failed = true);

        return failed ? null : bought;
    }

    // Settling checks the limit again, other purchases can get there first
    public async Task<(CurrencyExchangeStatus status, long exchangeId)> OpenCurrencyExchange(long characterGuid, CurrencyExchangeRate rate, int exchanges)
    {
        const string OPEN_SQL = @"
                INSERT INTO webapi.""CharacterCurrencyExchanges"" (character_guid, from_sdb_id, from_quantity, to_sdb_id, to_quantity)
                VALUES (@characterGuid, @FromSdbId, @fromQuantity, @ToSdbId, @toQuantity)
                RETURNING id";

        if (exchanges <= 0 || exchanges > rate.DailyLimit / rate.ToQuantity)
            return (CurrencyExchangeStatus.LimitReached, 0);

        var fromQuantity = exchanges * rate.FromQuantity;
        var toQuantity   = exchanges * rate.ToQuantity;

        var failed = false;
        var result = await DBCall(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();

            var bought = await conn.QueryFirstAsync<int>(BOUGHT_TODAY_SQL, new { characterGuid, rate.FromSdbId, rate.ToSdbId }, tx);
            if (bought + toQuantity > rate.DailyLimit)
                return (CurrencyExchangeStatus.LimitReached, 0L);

            var exchangeId = await conn.QueryFirstAsync<long>(OPEN_SQL, new { characterGuid, rate.FromSdbId, fromQuantity, rate.ToSdbId, toQuantity }, tx);
            await RequestWalletCharge(conn, tx, WalletChargeKinds.CURRENCY_EXCHANGE, exchangeId, characterGuid, rate.FromSdbId, fromQuantity);

            await tx.CommitAsync();
            return (CurrencyExchangeStatus.Pending, exchangeId);
        }, _ => failed = true);

        return failed ? (CurrencyExchangeStatus.Error, 0) : result;
    }

    private async Task<WalletChargeSettlement> SettleCurrencyExchange(long exchangeId, long characterGuid, bool paid, Guid paymentId)
    {
        const string REQUEST_SQL = @"
                SELECT status,
                       from_sdb_id AS FromSdbId, from_quantity AS FromQuantity, to_sdb_id AS ToSdbId, to_quantity AS ToQuantity,
                       created_at < now() - make_interval(secs => @staleSeconds) AS Stale
                FROM webapi.""CharacterCurrencyExchanges""
                WHERE id = @exchangeId
                AND character_guid = @characterGuid
                FOR UPDATE";

        const string SETTLE_SQL = @"
                UPDATE webapi.""CharacterCurrencyExchanges""
                SET status = @status,
                    exchange_day = CASE WHEN @status = 1 THEN (now() AT TIME ZONE 'UTC')::date END
                WHERE id = @exchangeId";

        var failed = false;
        var result = await DBCall(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();

            await conn.ExecuteAsync(CURRENCY_EXCHANGE_LOCK_SQL, new { characterGuid }, tx);

            var request = await conn.QueryFirstOrDefaultAsync<CurrencyExchangeRequestRow>(REQUEST_SQL,
                new { exchangeId, characterGuid, staleSeconds = (double)WALLET_CHARGE_STALE_SECONDS }, tx);

            if (request == null)
                return WalletChargeSettlement.Error;

            if (await FindWalletChargePayment(conn, tx, WalletChargeKinds.CURRENCY_EXCHANGE, exchangeId, paymentId) is { } previous)
                return previous;

            var pending = request.status == (short)CurrencyExchangeStatus.Pending;
            var status  = (CurrencyExchangeStatus)request.status;
            if (pending)
            {
                var rate = CurrencyExchangeRates.Find(request.FromSdbId, request.ToSdbId);
                if (request.Stale || rate == null)
                {
                    status = CurrencyExchangeStatus.NoAnswer;
                }
                else if (!paid)
                {
                    status = CurrencyExchangeStatus.NotEnough;
                }
                else
                {
                    var bought = await conn.QueryFirstAsync<int>(BOUGHT_TODAY_SQL, new { characterGuid, rate.FromSdbId, rate.ToSdbId }, tx);
                    status = bought + request.ToQuantity > rate.DailyLimit ? CurrencyExchangeStatus.LimitReached : CurrencyExchangeStatus.Exchanged;
                }
            }

            var kept    = pending && status == CurrencyExchangeStatus.Exchanged;
            var changes = new WalletChanges();

            if (kept)
                await changes.Deliver(conn, tx, characterGuid, request.ToSdbId, request.ToQuantity, "currency exchange");
            else if (paid)
                await changes.Deliver(conn, tx, characterGuid, request.FromSdbId, request.FromQuantity, "currency exchange refund");

            if (pending)
                await conn.ExecuteAsync(SETTLE_SQL, new { exchangeId, status = (short)status }, tx);

            var settlement = new WalletChargeSettlement(kept, (int)status);
            await RecordWalletChargePayment(conn, tx, WalletChargeKinds.CURRENCY_EXCHANGE, exchangeId, paymentId, characterGuid, paid, settlement);
            await changes.Notify(conn, tx);

            await tx.CommitAsync();
            return settlement;
        }, _ => failed = true);

        return failed ? WalletChargeSettlement.Error : result;
    }

    public async Task<CurrencyExchangeStatus> GetCurrencyExchangeStatus(long exchangeId)
    {
        const string SELECT_SQL = @"SELECT status FROM webapi.""CharacterCurrencyExchanges"" WHERE id = @exchangeId";

        var status = await DBCall(conn => conn.QueryFirstOrDefaultAsync<short?>(SELECT_SQL, new { exchangeId }));
        return status.HasValue ? (CurrencyExchangeStatus)status.Value : CurrencyExchangeStatus.Error;
    }

    // Settling takes the same lock, so either it settled first and its outcome comes back, or it finds the purchase
    // given up on and delivers the payment back
    public async Task<CurrencyExchangeStatus> AbandonCurrencyExchange(long exchangeId, long characterGuid)
    {
        const string ABANDON_SQL = @"
                UPDATE webapi.""CharacterCurrencyExchanges""
                SET status = 4
                WHERE id = @exchangeId
                AND status = 0";

        const string SELECT_SQL = @"SELECT status FROM webapi.""CharacterCurrencyExchanges"" WHERE id = @exchangeId";

        var failed = false;
        var result = await DBCall(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();

            await conn.ExecuteAsync(CURRENCY_EXCHANGE_LOCK_SQL, new { characterGuid }, tx);
            await conn.ExecuteAsync(ABANDON_SQL, new { exchangeId }, tx);
            var status = await conn.QueryFirstAsync<short>(SELECT_SQL, new { exchangeId }, tx);

            await tx.CommitAsync();
            return (CurrencyExchangeStatus)status;
        }, _ => failed = true);

        return failed ? CurrencyExchangeStatus.Error : result;
    }

    private sealed class CurrencyExchangeRequestRow
    {
        public short status         { get; set; }
        public int   FromSdbId      { get; set; }
        public int   FromQuantity   { get; set; }
        public int   ToSdbId        { get; set; }
        public int   ToQuantity     { get; set; }
        public bool  Stale          { get; set; }
    }
}
