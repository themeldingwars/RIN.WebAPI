using Dapper;
using Npgsql;
using static RIN.Core.DB.RedBeanExchangeRules;

namespace RIN.Core.DB;

/// <summary>
/// Orders only trade when a new one meets an order already on the book. A new buy order takes the cheapest sell orders
/// at or under its price, a new sell order the dearest buy orders at or over its price, the oldest first at a price, and
/// each trade is at the price of the order that was already on the book. Whatever doesn't trade stays on the book. When
/// every sell order asks more than any buy order offers, nothing trades until someone meets a price.
///
/// A sell order holds its red beans out of the account's rb_balance when it is placed. A buy order holds
/// rb * price_per_unit credits, which the game server hosting the character takes through a wallet charge (see
/// DB.Wallet); it only reaches the book once they are taken. A buyer gets its red beans on the account, plus the
/// credits back when it bought under its own price; a seller gets its credits as a delivery. Cancelling gives back
/// whatever hadn't traded.
///
/// Everything that changes the book takes one lock, so orders trade one at a time.
/// </summary>
public partial class DB
{
    private const string RED_BEAN_BOOK_LOCK_SQL = @"SELECT pg_advisory_xact_lock(4502, 0)";

    private const string RED_BEAN_ORDER_COLUMNS = @"id, character_guid, account_id, operation, rb, rb_remaining, price_per_unit, status, created_at, updated_at";

    // Deletion is a row in DeletionQueue; Characters.deleted_at is checked as well, like at login
    private const string RED_BEAN_CHARACTER_ACCOUNT_SQL = @"
            SELECT c.account_id
            FROM webapi.""Characters"" c
            WHERE c.character_guid = @characterGuid
            AND c.deleted_at IS NULL
            AND NOT EXISTS (SELECT 1 FROM webapi.""DeletionQueue"" dq WHERE dq.character_guid = c.character_guid)";

    private const string RED_BEAN_OPEN_ORDERS_SQL = @"
            SELECT count(*)::integer
            FROM webapi.""RedBeanExchangeOrders""
            WHERE character_guid = @characterGuid
            AND status IN (0, 2)";

    private const string RED_BEAN_INSERT_ORDER_SQL = @"
            INSERT INTO webapi.""RedBeanExchangeOrders"" (character_guid, account_id, operation, rb, rb_remaining, price_per_unit, status)
            VALUES (@characterGuid, @accountId, @operation, @rb, @rb, @pricePerUnit, @status)
            RETURNING " + RED_BEAN_ORDER_COLUMNS;

    private static readonly string RED_BEAN_ORDER_FOR_UPDATE_SQL = $@"
            SELECT {RED_BEAN_ORDER_COLUMNS}
            FROM webapi.""RedBeanExchangeOrders""
            WHERE id = @orderId
            AND character_guid = @characterGuid
            FOR UPDATE";

    public async Task<List<RedBeanBookLevelRow>?> GetRedBeanExchangeBook()
    {
        const string BOOK_SQL = @"
                SELECT operation, price_per_unit, rb
                FROM (
                    SELECT operation, price_per_unit, SUM(rb_remaining)::bigint AS rb,
                           row_number() OVER (PARTITION BY operation
                                              ORDER BY CASE WHEN operation = 'sell' THEN price_per_unit ELSE -price_per_unit END) AS level
                    FROM webapi.""RedBeanExchangeOrders""
                    WHERE status = 2
                    GROUP BY operation, price_per_unit
                ) levels
                WHERE level <= @depth
                ORDER BY operation, level";

        var failed = false;
        var book = await DBCall(async conn => (await conn.QueryAsync<RedBeanBookLevelRow>(BOOK_SQL, new { depth = BOOK_DEPTH })).ToList(),
            _ => failed = true);

        return failed ? null : book;
    }

    public async Task<List<RedBeanExchangeOrderRow>?> GetRedBeanExchangeOrders(long characterGuid, bool open)
    {
        var sql = open
            ? $@"SELECT {RED_BEAN_ORDER_COLUMNS}
                 FROM webapi.""RedBeanExchangeOrders""
                 WHERE character_guid = @characterGuid
                 AND status IN (0, 2)
                 ORDER BY created_at DESC, id DESC
                 LIMIT @limit"
            : $@"SELECT {RED_BEAN_ORDER_COLUMNS}
                 FROM webapi.""RedBeanExchangeOrders""
                 WHERE character_guid = @characterGuid
                 AND status IN (3, 4, 5)
                 ORDER BY updated_at DESC, id DESC
                 LIMIT @limit";

        var limit = open ? MAX_OPEN_ORDERS : HISTORY_LENGTH;

        var failed = false;
        var orders = await DBCall(async conn => (await conn.QueryAsync<RedBeanExchangeOrderRow>(sql, new { characterGuid, limit })).ToList(),
            _ => failed = true);

        return failed ? null : orders;
    }

    public async Task<(RedBeanOrderOutcome outcome, RedBeanExchangeOrderRow? order)> PlaceRedBeanSellOrder(long characterGuid, int rb, int pricePerUnit)
    {
        const string HOLD_SQL = @"
                UPDATE webapi.""Accounts""
                SET rb_balance = rb_balance - @rb
                WHERE account_id = @accountId
                AND rb_balance >= @rb
                RETURNING account_id";

        var failed = false;
        var result = await DBCall<(RedBeanOrderOutcome, RedBeanExchangeOrderRow?)>(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();

            await conn.ExecuteAsync(RED_BEAN_BOOK_LOCK_SQL, transaction: tx);

            var accountId = await conn.QueryFirstOrDefaultAsync<long?>(RED_BEAN_CHARACTER_ACCOUNT_SQL, new { characterGuid }, tx);
            if (accountId == null)
                return (RedBeanOrderOutcome.NotFound, null);

            if (await conn.QueryFirstAsync<int>(RED_BEAN_OPEN_ORDERS_SQL, new { characterGuid }, tx) >= MAX_OPEN_ORDERS)
                return (RedBeanOrderOutcome.TooManyOrders, null);

            if (await conn.QueryFirstOrDefaultAsync<long?>(HOLD_SQL, new { accountId, rb = (long)rb }, tx) == null)
                return (RedBeanOrderOutcome.NotEnough, null);

            var order = await conn.QueryFirstAsync<RedBeanExchangeOrderRow>(RED_BEAN_INSERT_ORDER_SQL,
                new { characterGuid, accountId, operation = SELL, rb, pricePerUnit, status = RedBeanOrderStatus.ACTIVE }, tx);

            var changes = new WalletChanges();
            await MatchRedBeanOrder(conn, tx, order, changes);
            await changes.Notify(conn, tx);

            await tx.CommitAsync();
            return (RedBeanOrderOutcome.Placed, order);
        }, _ => failed = true);

        return failed ? (RedBeanOrderOutcome.Error, null) : result;
    }

    // It reaches the book when the game server settles the charge
    public async Task<(RedBeanOrderOutcome outcome, long orderId)> OpenRedBeanBuyOrder(long characterGuid, int rb, int pricePerUnit)
    {
        var failed = false;
        var result = await DBCall(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();

            await conn.ExecuteAsync(RED_BEAN_BOOK_LOCK_SQL, transaction: tx);

            var accountId = await conn.QueryFirstOrDefaultAsync<long?>(RED_BEAN_CHARACTER_ACCOUNT_SQL, new { characterGuid }, tx);
            if (accountId == null)
                return (RedBeanOrderOutcome.NotFound, 0L);

            if (await conn.QueryFirstAsync<int>(RED_BEAN_OPEN_ORDERS_SQL, new { characterGuid }, tx) >= MAX_OPEN_ORDERS)
                return (RedBeanOrderOutcome.TooManyOrders, 0L);

            var order = await conn.QueryFirstAsync<RedBeanExchangeOrderRow>(RED_BEAN_INSERT_ORDER_SQL,
                new { characterGuid, accountId, operation = BUY, rb, pricePerUnit, status = RedBeanOrderStatus.PENDING }, tx);

            await RequestWalletCharge(conn, tx, WalletChargeKinds.RED_BEAN_BUY_ORDER, order.id, characterGuid, CurrencyExchangeRates.CREDITS_SDB_ID,
                (long)rb * pricePerUnit);

            await tx.CommitAsync();
            return (RedBeanOrderOutcome.Pending, order.id);
        }, _ => failed = true);

        return failed ? (RedBeanOrderOutcome.Error, 0) : result;
    }

    private async Task<WalletChargeSettlement> SettleRedBeanBuyOrder(long orderId, long characterGuid, bool paid, Guid paymentId)
    {
        const string STALE_SQL = @"SELECT @createdAt < now() - make_interval(secs => @staleSeconds)";

        const string SETTLE_SQL = @"
                UPDATE webapi.""RedBeanExchangeOrders""
                SET status = @status,
                    updated_at = now()
                WHERE id = @orderId";

        var failed = false;
        var result = await DBCall(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();

            await conn.ExecuteAsync(RED_BEAN_BOOK_LOCK_SQL, transaction: tx);

            var order = await conn.QueryFirstOrDefaultAsync<RedBeanExchangeOrderRow>(RED_BEAN_ORDER_FOR_UPDATE_SQL, new { orderId, characterGuid }, tx);
            if (order == null || !order.IsBuy)
                return WalletChargeSettlement.Error;

            if (await FindWalletChargePayment(conn, tx, WalletChargeKinds.RED_BEAN_BUY_ORDER, orderId, paymentId) is { } previous)
                return previous;

            var changes = new WalletChanges();
            var pending = order.status == RedBeanOrderStatus.PENDING;
            var status  = order.status;

            if (pending)
            {
                var stale = await conn.QueryFirstAsync<bool>(STALE_SQL,
                    new { createdAt = order.created_at, staleSeconds = (double)WALLET_CHARGE_STALE_SECONDS }, tx);

                status = paid && !stale ? RedBeanOrderStatus.ACTIVE : RedBeanOrderStatus.FAILED;
            }

            var kept = pending && status == RedBeanOrderStatus.ACTIVE;

            if (!kept && paid)
            {
                await changes.Deliver(conn, tx, characterGuid, CurrencyExchangeRates.CREDITS_SDB_ID, (long)order.rb * order.price_per_unit,
                    "red bean buy order refund");
            }

            if (pending)
                await conn.ExecuteAsync(SETTLE_SQL, new { orderId, status }, tx);

            if (kept)
            {
                order.status = status;
                await MatchRedBeanOrder(conn, tx, order, changes);
            }

            var settlement = new WalletChargeSettlement(kept, status);
            await RecordWalletChargePayment(conn, tx, WalletChargeKinds.RED_BEAN_BUY_ORDER, orderId, paymentId, characterGuid, paid, settlement);
            await changes.Notify(conn, tx);

            await tx.CommitAsync();
            return settlement;
        }, _ => failed = true);

        return failed ? WalletChargeSettlement.Error : result;
    }

    public async Task<short?> GetRedBeanOrderStatus(long orderId)
    {
        const string SELECT_SQL = @"SELECT status FROM webapi.""RedBeanExchangeOrders"" WHERE id = @orderId";

        return await DBCall(conn => conn.QueryFirstOrDefaultAsync<short?>(SELECT_SQL, new { orderId }));
    }

    // Settling takes the same lock, so either it settled first and its outcome comes back, or it finds the order failed
    // and delivers the credits back
    public async Task<(bool abandoned, short status)?> AbandonRedBeanBuyOrder(long orderId)
    {
        const string ABANDON_SQL = @"
                UPDATE webapi.""RedBeanExchangeOrders""
                SET status = 4,
                    updated_at = now()
                WHERE id = @orderId
                AND status = 0";

        const string SELECT_SQL = @"SELECT status FROM webapi.""RedBeanExchangeOrders"" WHERE id = @orderId";

        var failed = false;
        var result = await DBCall(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();

            await conn.ExecuteAsync(RED_BEAN_BOOK_LOCK_SQL, transaction: tx);
            var abandoned = await conn.ExecuteAsync(ABANDON_SQL, new { orderId }, tx) > 0;
            var status    = await conn.QueryFirstAsync<short>(SELECT_SQL, new { orderId }, tx);

            await tx.CommitAsync();
            return (abandoned, status);
        }, _ => failed = true);

        return failed ? null : result;
    }

    public async Task<(RedBeanOrderOutcome outcome, RedBeanExchangeOrderRow? order)> CancelRedBeanExchangeOrder(long characterGuid, long orderId)
    {
        const string RETURN_RED_BEANS_SQL = @"
                UPDATE webapi.""Accounts""
                SET rb_balance = rb_balance + @rb
                WHERE account_id = @accountId";

        const string CANCEL_SQL = @"
                UPDATE webapi.""RedBeanExchangeOrders""
                SET status = 5,
                    updated_at = now()
                WHERE id = @orderId
                RETURNING " + RED_BEAN_ORDER_COLUMNS;

        var failed = false;
        var result = await DBCall<(RedBeanOrderOutcome, RedBeanExchangeOrderRow?)>(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();

            await conn.ExecuteAsync(RED_BEAN_BOOK_LOCK_SQL, transaction: tx);

            var order = await conn.QueryFirstOrDefaultAsync<RedBeanExchangeOrderRow>(RED_BEAN_ORDER_FOR_UPDATE_SQL, new { orderId, characterGuid }, tx);
            if (order == null)
                return (RedBeanOrderOutcome.NotFound, null);

            if (order.status != RedBeanOrderStatus.ACTIVE)
                return (RedBeanOrderOutcome.NotActive, order);

            var changes = new WalletChanges();
            if (order.IsBuy)
            {
                await changes.Deliver(conn, tx, characterGuid, CurrencyExchangeRates.CREDITS_SDB_ID, (long)order.rb_remaining * order.price_per_unit,
                    "red bean buy order cancelled");
            }
            else if (order.rb_remaining > 0)
            {
                await conn.ExecuteAsync(RETURN_RED_BEANS_SQL, new { accountId = order.account_id, rb = (long)order.rb_remaining }, tx);
            }

            order = await conn.QueryFirstAsync<RedBeanExchangeOrderRow>(CANCEL_SQL, new { orderId }, tx);
            await changes.Notify(conn, tx);

            await tx.CommitAsync();
            return (RedBeanOrderOutcome.Cancelled, order);
        }, _ => failed = true);

        return failed ? (RedBeanOrderOutcome.Error, null) : result;
    }

    // Called under the book lock
    private static async Task MatchRedBeanOrder(NpgsqlConnection conn, NpgsqlTransaction tx, RedBeanExchangeOrderRow order, WalletChanges changes)
    {
        var restingSql = order.IsBuy
            ? $@"SELECT {RED_BEAN_ORDER_COLUMNS}
                 FROM webapi.""RedBeanExchangeOrders""
                 WHERE status = 2
                 AND operation = 'sell'
                 AND price_per_unit <= @price
                 ORDER BY price_per_unit, created_at, id"
            : $@"SELECT {RED_BEAN_ORDER_COLUMNS}
                 FROM webapi.""RedBeanExchangeOrders""
                 WHERE status = 2
                 AND operation = 'buy'
                 AND price_per_unit >= @price
                 ORDER BY price_per_unit DESC, created_at, id";

        const string TRADE_SQL = @"
                INSERT INTO webapi.""RedBeanExchangeTrades"" (buy_order_id, sell_order_id, rb, price_per_unit)
                VALUES (@buyOrderId, @sellOrderId, @rb, @price)";

        const string FILL_SQL = @"
                UPDATE webapi.""RedBeanExchangeOrders""
                SET rb_remaining = @remaining,
                    status = CASE WHEN @remaining = 0 THEN 3 ELSE status END,
                    updated_at = now()
                WHERE id = @id";

        const string GIVE_RED_BEANS_SQL = @"
                UPDATE webapi.""Accounts""
                SET rb_balance = rb_balance + @rb
                WHERE account_id = @accountId";

        var resting = await conn.QueryAsync<RedBeanExchangeOrderRow>(restingSql, new { price = order.price_per_unit }, tx);

        foreach (var other in resting)
        {
            if (order.rb_remaining == 0)
                break;

            var rb    = Math.Min(order.rb_remaining, other.rb_remaining);
            var price = other.price_per_unit;
            var (buy, sell) = order.IsBuy ? (order, other) : (other, order);

            order.rb_remaining -= rb;
            other.rb_remaining -= rb;

            await conn.ExecuteAsync(TRADE_SQL, new { buyOrderId = buy.id, sellOrderId = sell.id, rb, price }, tx);
            await conn.ExecuteAsync(FILL_SQL, new { other.id, remaining = other.rb_remaining }, tx);

            await conn.ExecuteAsync(GIVE_RED_BEANS_SQL, new { accountId = buy.account_id, rb = (long)rb }, tx);

            await changes.Deliver(conn, tx, sell.character_guid, CurrencyExchangeRates.CREDITS_SDB_ID, (long)rb * price, "red bean sale");

            // The buy order held its own price for every red bean, it gets back what it didn't have to pay
            await changes.Deliver(conn, tx, buy.character_guid, CurrencyExchangeRates.CREDITS_SDB_ID, (long)rb * (buy.price_per_unit - price),
                "red bean purchase change");
        }

        await conn.ExecuteAsync(FILL_SQL, new { order.id, remaining = order.rb_remaining }, tx);
        if (order.rb_remaining == 0)
            order.status = RedBeanOrderStatus.COMPLETED;
    }
}
