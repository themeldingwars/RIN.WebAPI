--
-- Purchases on the currency exchange, and the currency waiting to be handed to characters.
--
-- CharacterCurrencyExchanges: one row per purchase. from_quantity is what the character pays, to_quantity what it
-- gets. status:
--   0 pending, waiting for the game server
--   1 exchanged, the game server took the payment and the purchase was delivered
--   2 the character couldn't pay for it
--   3 it would have gone over the day's limit (no longer set: the limit is checked before a purchase is stored)
--   4 no game server answered in time, the character is offline or between zones
-- exchange_day is the UTC day a purchase was opened on, kept for the exchanged ones and NULL for the failed ones. The
-- daily limit counts the exchanged ones and the pending ones that can still go through, so opening a purchase reserves
-- its share; it resets at midnight UTC, the same moment the client's countdown runs out.
--
-- CharacterCurrencyDeliveries: currency owed to a character, sdb_id being the resource. claimed_at is set when a game
-- server put it in the character's wallet.
--
-- WalletChargePayments: every payment a game server took, or failed to take, for a wallet charge (kind 1 a purchase
-- here, kind 2 a red bean buy order), under the id the game server gave it.
--
-- Safe to run twice.

BEGIN;

CREATE TABLE IF NOT EXISTS webapi."CharacterCurrencyExchanges" (
    id bigint GENERATED ALWAYS AS IDENTITY,
    character_guid bigint NOT NULL,
    from_sdb_id integer NOT NULL,
    from_quantity integer NOT NULL,
    to_sdb_id integer NOT NULL,
    to_quantity integer NOT NULL,
    status smallint DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    exchange_day date,
    CONSTRAINT "CharacterCurrencyExchanges_pkey" PRIMARY KEY (id),
    CONSTRAINT "CharacterCurrencyExchanges_status" CHECK (((status >= 0) AND (status <= 4))),
    CONSTRAINT character_currency_exchanges_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE
);

ALTER TABLE webapi."CharacterCurrencyExchanges" OWNER TO tmwadmin;

-- Serves the daily limit, and the cascade when a character is deleted
CREATE INDEX IF NOT EXISTS idx_character_currency_exchanges_day ON webapi."CharacterCurrencyExchanges" USING btree (character_guid, exchange_day);

CREATE TABLE IF NOT EXISTS webapi."CharacterCurrencyDeliveries" (
    id bigint GENERATED ALWAYS AS IDENTITY,
    character_guid bigint NOT NULL,
    sdb_id integer NOT NULL,
    quantity integer NOT NULL,
    reason text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    claimed_at timestamp with time zone,
    CONSTRAINT "CharacterCurrencyDeliveries_pkey" PRIMARY KEY (id),
    CONSTRAINT "CharacterCurrencyDeliveries_quantity" CHECK ((quantity > 0)),
    CONSTRAINT character_currency_deliveries_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE
);

ALTER TABLE webapi."CharacterCurrencyDeliveries" OWNER TO tmwadmin;

-- Finds what a character still has to claim, and serves the cascade when a character is deleted
CREATE INDEX IF NOT EXISTS idx_character_currency_deliveries_unclaimed ON webapi."CharacterCurrencyDeliveries" USING btree (character_guid) WHERE (claimed_at IS NULL);

CREATE TABLE IF NOT EXISTS webapi."WalletChargePayments" (
    kind smallint NOT NULL,
    charge_id bigint NOT NULL,
    payment_id uuid NOT NULL,
    character_guid bigint NOT NULL,
    paid boolean NOT NULL,
    kept boolean NOT NULL,
    status smallint NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT "WalletChargePayments_pkey" PRIMARY KEY (kind, charge_id, payment_id),
    CONSTRAINT wallet_charge_payments_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE
);

ALTER TABLE webapi."WalletChargePayments" OWNER TO tmwadmin;

-- Serves the cascade when a character is deleted
CREATE INDEX IF NOT EXISTS idx_wallet_charge_payments_character ON webapi."WalletChargePayments" USING btree (character_guid);

COMMIT;
