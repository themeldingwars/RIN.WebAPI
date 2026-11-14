--
-- The red bean exchange: buy and sell orders for red beans, at 100 to 500 credits each.
--
-- Run after 2026-11-14-currency-exchange.sql, which adds the deliveries the credits are handed out through.
--
-- RedBeanExchangeOrders: operation is the character's side, 'buy' or 'sell'. rb is how many red beans the order is
-- for, rb_remaining how many are still to trade. A sell order holds its red beans out of the account's rb_balance, a
-- buy order holds rb * price_per_unit credits out of the character's wallet. status is the client's:
--   0 pending, a buy order waiting for the game server to take the credits
--   2 active, on the book
--   3 completed, all of it traded
--   4 failed, a buy order whose credits couldn't be taken
--   5 cancelled, whatever hadn't traded was given back
--
-- RedBeanExchangeTrades: every time two orders trade, how many red beans at what price.
--
-- Safe to run twice.

BEGIN;

CREATE TABLE IF NOT EXISTS webapi."RedBeanExchangeOrders" (
    id bigint GENERATED ALWAYS AS IDENTITY,
    character_guid bigint NOT NULL,
    account_id bigint NOT NULL,
    operation text NOT NULL,
    rb integer NOT NULL,
    rb_remaining integer NOT NULL,
    price_per_unit integer NOT NULL,
    status smallint DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT "RedBeanExchangeOrders_pkey" PRIMARY KEY (id),
    CONSTRAINT "RedBeanExchangeOrders_operation" CHECK ((operation = ANY (ARRAY['buy'::text, 'sell'::text]))),
    CONSTRAINT "RedBeanExchangeOrders_rb" CHECK (((rb > 0) AND (rb_remaining >= 0) AND (rb_remaining <= rb))),
    CONSTRAINT "RedBeanExchangeOrders_price_per_unit" CHECK (((price_per_unit >= 100) AND (price_per_unit <= 500))),
    CONSTRAINT "RedBeanExchangeOrders_status" CHECK ((status = ANY (ARRAY[0, 2, 3, 4, 5]))),
    CONSTRAINT red_bean_exchange_orders_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE
);

ALTER TABLE webapi."RedBeanExchangeOrders" OWNER TO tmwadmin;

-- The book: the active orders of a side by price
CREATE INDEX IF NOT EXISTS idx_red_bean_exchange_orders_book ON webapi."RedBeanExchangeOrders" USING btree (operation, price_per_unit, created_at) WHERE (status = 2);

-- A character's orders, newest first
CREATE INDEX IF NOT EXISTS idx_red_bean_exchange_orders_character ON webapi."RedBeanExchangeOrders" USING btree (character_guid, updated_at);

CREATE TABLE IF NOT EXISTS webapi."RedBeanExchangeTrades" (
    id bigint GENERATED ALWAYS AS IDENTITY,
    buy_order_id bigint NOT NULL,
    sell_order_id bigint NOT NULL,
    rb integer NOT NULL,
    price_per_unit integer NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT "RedBeanExchangeTrades_pkey" PRIMARY KEY (id),
    CONSTRAINT red_bean_exchange_trades_buy_order_id_fkey FOREIGN KEY (buy_order_id) REFERENCES webapi."RedBeanExchangeOrders"(id) ON DELETE CASCADE,
    CONSTRAINT red_bean_exchange_trades_sell_order_id_fkey FOREIGN KEY (sell_order_id) REFERENCES webapi."RedBeanExchangeOrders"(id) ON DELETE CASCADE
);

ALTER TABLE webapi."RedBeanExchangeTrades" OWNER TO tmwadmin;

CREATE INDEX IF NOT EXISTS idx_red_bean_exchange_trades_buy ON webapi."RedBeanExchangeTrades" USING btree (buy_order_id);
CREATE INDEX IF NOT EXISTS idx_red_bean_exchange_trades_sell ON webapi."RedBeanExchangeTrades" USING btree (sell_order_id);

-- Announces every change of an account's red beans, whatever made it, so game servers show the new balance
CREATE OR REPLACE FUNCTION webapi.notify_on_red_beans_update() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    PERFORM pg_notify('events', 'RedBeansUpdated->' || json_build_object('AccountId', NEW.account_id, 'Balance', NEW.rb_balance)::text);
    RETURN NULL;
END;
$$;

ALTER FUNCTION webapi.notify_on_red_beans_update() OWNER TO tmwadmin;

DROP TRIGGER IF EXISTS red_beans_update_trigger ON webapi."Accounts";
CREATE TRIGGER red_beans_update_trigger AFTER UPDATE OF rb_balance ON webapi."Accounts" FOR EACH ROW WHEN ((old.rb_balance IS DISTINCT FROM new.rb_balance)) EXECUTE FUNCTION webapi.notify_on_red_beans_update();

COMMIT;
