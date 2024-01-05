--
-- PostgreSQL database dump
--

-- Dumped from database version 14.1
-- Dumped by pg_dump version 14.1

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: webapi; Type: SCHEMA; Schema: -; Owner: tmwadmin
--

CREATE SCHEMA webapi;


ALTER SCHEMA webapi OWNER TO tmwadmin;

--
-- Name: SCHEMA webapi; Type: COMMENT; Schema: -; Owner: tmwadmin
--

COMMENT ON SCHEMA webapi IS 'standard public schema';


--
-- Name: CreateNewAccount(text, text, date, boolean, text, text, bytea); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi."CreateNewAccount"(email text, country text, birthday date, email_optin boolean, uid text, secret text, password_hash bytea, OUT error_text text, OUT new_account_id bigint) RETURNS record
    LANGUAGE plpgsql
    AS $$
declare
    c text;
BEGIN

    -- Check if the email is already in use, the constraint will do this but want to avoid incrementing the sequence on fails
    IF (SELECT exists (SELECT 1 FROM webapi."Accounts" WHERE "Accounts".email = "CreateNewAccount".email)) THEN
        new_account_id = -1;
        error_text = 'ERR_ACCOUNT_EXISTS';
        RETURN;
    END IF;

    INSERT INTO webapi."Accounts" (email, uid, password_hash, birthday, country, secret, email_optin,
                                   created_at, last_login, email_verified, is_dev, character_limit, rb_balance)
    VALUES (email, uid, password_hash, birthday, country, secret, email_optin,
            current_timestamp, '-infinity', false, false, -1, 0) RETURNING account_id INTO new_account_id;

    EXCEPTION WHEN OTHERS THEN
        GET STACKED DIAGNOSTICS c := CONSTRAINT_NAME;
        IF c = 'accounts_email_uindex' THEN
            error_text = 'ERR_ACCOUNT_EXISTS';
        ELSE
            error_text = 'ERR_UNKNOWN';
            RAISE;
        END IF;
END
$$;


ALTER FUNCTION webapi."CreateNewAccount"(email text, country text, birthday date, email_optin boolean, uid text, secret text, password_hash bytea, OUT error_text text, OUT new_account_id bigint) OWNER TO tmwadmin;

--
-- Name: FUNCTION "CreateNewAccount"(email text, country text, birthday date, email_optin boolean, uid text, secret text, password_hash bytea, OUT error_text text, OUT new_account_id bigint); Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON FUNCTION webapi."CreateNewAccount"(email text, country text, birthday date, email_optin boolean, uid text, secret text, password_hash bytea, OUT error_text text, OUT new_account_id bigint) IS 'Create a new account for a user and the default tables and data for it';


--
-- Name: CreateNewCharacter(bigint, text, boolean, integer, integer, integer, bytea); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi."CreateNewCharacter"(account_id bigint, name text, is_dev boolean, voice_setid integer, gender integer, visuals bytea, OUT error_text text, OUT new_character_id bigint) RETURNS record
    LANGUAGE plpgsql
    AS $$
declare
    c text;
BEGIN

    -- Check if the name isn't used
    IF (SELECT exists(SELECT 1 FROM webapi."Characters" WHERE webapi."Characters".name = "CreateNewCharacter".name)) THEN
        new_character_id = -1;
        error_text = 'ERR_NAME_IN_USE';
        RETURN;
    END IF;

    insert into webapi."Characters" (character_guid,
                                     name,
                                     unique_name,
                                     is_dev,
                                     is_active,
                                     account_id,
                                     created_at,
                                     title_id,
                                     time_played_secs,
                                     needs_name_change,
                                     gender,
                                     last_seen_at,
                                     race,
                                     visuals)
    values (webapi.create_entity_guid(254),
      name,
            UPPER(name),
            (is_dev AND (SELECT webapi."Accounts".is_dev FROM webapi."Accounts" WHERE webapi."Accounts".account_id = "CreateNewCharacter".account_id)),
            true,
            "CreateNewCharacter".account_id,
            current_timestamp,
            0,
            0,
            false,
            gender,
            current_timestamp,
            0,
            visuals)
    RETURNING character_guid INTO new_character_id;

    error_text = '';

END
$$;


ALTER FUNCTION webapi."CreateNewCharacter"(account_id bigint, name text, is_dev boolean, voice_setid integer, gender integer, current_battleframe_id integer, visuals bytea, OUT error_text text, OUT new_character_id bigint) OWNER TO tmwadmin;

--
-- Name: create_entity_guid(integer); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi.create_entity_guid("Type" integer) RETURNS bigint
    LANGUAGE plpgsql
    AS $$DECLARE
counter bigint;
serverId bigint;
timestamp bigint;
BEGIN

counter = nextval('webapi."FFUUID_Counter_Seq"'::regclass);
serverId = (1::bigint << 56);
timestamp = (((extract(epoch from pg_postmaster_start_time())::bigint >> 8) & x'00FFFFFF'::bigint << 32));
RETURN serverId + timestamp + (counter << 8) + "Type";

END$$;


ALTER FUNCTION webapi.create_entity_guid("Type" integer) OWNER TO tmwadmin;

--
-- Name: FUNCTION create_entity_guid("Type" integer); Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON FUNCTION webapi.create_entity_guid("Type" integer) IS 'Create an entity id for example for a character, pass in a number for the type

Based on https://gist.github.com/SilentCLD/881839a9f45578f1618db012fc789a71 ';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: Accounts; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."Accounts" (
    account_id bigint NOT NULL,
    is_dev boolean DEFAULT false NOT NULL,
    character_limit smallint,
    email text NOT NULL,
    uid text NOT NULL,
    password_hash bytea NOT NULL,
    created_at timestamp with time zone NOT NULL,
    last_login timestamp with time zone,
    birthday date NOT NULL,
    country character(2) NOT NULL,
    secret text NOT NULL,
    email_optin boolean DEFAULT false NOT NULL,
    email_verified boolean DEFAULT false NOT NULL,
    rb_balance bigint NOT NULL
);


ALTER TABLE webapi."Accounts" OWNER TO tmwadmin;

--
-- Name: Accounts_account_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE webapi."Accounts" ALTER COLUMN account_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME webapi."Accounts_account_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Battleframes; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."Battleframes" (
    id bigint NOT NULL,
    character_guid bigint NOT NULL,
    battleframe_sdb_id integer NOT NULL,
    visuals bytea NOT NULL,
    hidden boolean DEFAULT false NOT NULL,
    level integer DEFAULT 1 NOT NULL,
    xp bigint DEFAULT 0 NOT NULL
);


ALTER TABLE webapi."Battleframes" OWNER TO tmwadmin;

--
-- Name: Battleframes_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE webapi."Battleframes" ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME webapi."Battleframes_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Characters; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."Characters" (
    character_guid bigint NOT NULL,
    name text NOT NULL,
    unique_name text NOT NULL,
    is_dev boolean DEFAULT false NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    account_id bigint NOT NULL,
    created_at timestamp with time zone NOT NULL,
    title_id integer,
    time_played_secs integer,
    needs_name_change boolean DEFAULT false NOT NULL,
    gender smallint DEFAULT 0 NOT NULL,
    last_seen_at timestamp with time zone NOT NULL,
    race smallint NOT NULL,
    visuals bytea NOT NULL,
    deleted_at timestamp with time zone,
    expires_in timestamp with time zone,
    current_battleframe_guid bigint
);


ALTER TABLE webapi."Characters" OWNER TO tmwadmin;

--
-- Name: ClientEvents; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."ClientEvents" (
    id bigint NOT NULL,
    event smallint,
    action text,
    message text,
    source text,
    user_id bigint,
    data text,
    date timestamp with time zone
);


ALTER TABLE webapi."ClientEvents" OWNER TO tmwadmin;

--
-- Name: ClientEvents_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE webapi."ClientEvents" ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME webapi."ClientEvents_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: Costs; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."Costs" (
    id bigint NOT NULL,
    name text NOT NULL,
    price integer NOT NULL,
    description text,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL
);


ALTER TABLE webapi."Costs" OWNER TO tmwadmin;

--
-- Name: DeletionQueue; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."DeletionQueue" (
    character_guid bigint NOT NULL,
    account_id bigint NOT NULL,
    deleted_at timestamp with time zone NOT NULL,
    expires_in timestamp with time zone NOT NULL
);


ALTER TABLE webapi."DeletionQueue" OWNER TO tmwadmin;

--
-- Name: FFUUID_Counter_Seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

CREATE SEQUENCE webapi."FFUUID_Counter_Seq"
    START WITH 0
    INCREMENT BY 1
    MINVALUE 0
    MAXVALUE 16777215
    CACHE 1;


ALTER TABLE webapi."FFUUID_Counter_Seq" OWNER TO tmwadmin;

--
-- Name: LoginEvents; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."LoginEvents" (
    id bigint NOT NULL,
    name text,
    description text,
    color text,
    is_active boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL
);


ALTER TABLE webapi."LoginEvents" OWNER TO tmwadmin;

--
-- Name: Purchases; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."Purchases" (
    account_id bigint NOT NULL,
    purchase_id bigint NOT NULL
);


ALTER TABLE webapi."Purchases" OWNER TO tmwadmin;

--
-- Name: Purchases_account_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE webapi."Purchases" ALTER COLUMN account_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME webapi."Purchases_account_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: VipData; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."VipData" (
    account_id bigint NOT NULL,
    start_date timestamp with time zone NOT NULL,
    expiration_date timestamp with time zone NOT NULL
);


ALTER TABLE webapi."VipData" OWNER TO tmwadmin;

--
-- Name: TABLE "VipData"; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON TABLE webapi."VipData" IS 'vip data, if the user has vip they should have a row here';


--
-- Name: ZoneCertificates; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."ZoneCertificates" (
    id integer NOT NULL,
    cert_id smallint NOT NULL,
    zone_setting_id smallint NOT NULL,
    authorize_position text NOT NULL,
    difficulty_key text,
    presence text NOT NULL
);


ALTER TABLE webapi."ZoneCertificates" OWNER TO tmwadmin;

--
-- Name: ZoneCertificates_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

CREATE SEQUENCE webapi."ZoneCertificates_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE webapi."ZoneCertificates_id_seq" OWNER TO tmwadmin;

--
-- Name: ZoneCertificates_id_seq; Type: SEQUENCE OWNED BY; Schema: webapi; Owner: tmwadmin
--

ALTER SEQUENCE webapi."ZoneCertificates_id_seq" OWNED BY webapi."ZoneCertificates".id;


--
-- Name: ZoneDifficulty; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."ZoneDifficulty" (
    id integer NOT NULL,
    zone_setting_id smallint NOT NULL,
    difficulty_key text NOT NULL,
    ui_string text NOT NULL,
    display_level smallint NOT NULL,
    min_level smallint NOT NULL,
    max_suggested_level smallint NOT NULL,
    min_players smallint NOT NULL,
    max_players smallint NOT NULL,
    min_players_accept smallint NOT NULL,
    group_min_players smallint NOT NULL,
    group_max_players smallint NOT NULL
);


ALTER TABLE webapi."ZoneDifficulty" OWNER TO tmwadmin;

--
-- Name: ZoneDifficulty_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

CREATE SEQUENCE webapi."ZoneDifficulty_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE webapi."ZoneDifficulty_id_seq" OWNER TO tmwadmin;

--
-- Name: ZoneDifficulty_id_seq; Type: SEQUENCE OWNED BY; Schema: webapi; Owner: tmwadmin
--

ALTER SEQUENCE webapi."ZoneDifficulty_id_seq" OWNED BY webapi."ZoneDifficulty".id;


--
-- Name: ZoneSettings; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."ZoneSettings" (
    id integer NOT NULL,
    zone_id integer DEFAULT 0 NOT NULL,
    mission_id smallint,
    gametype text NOT NULL,
    instance_type_pool text NOT NULL,
    is_preview_zone boolean DEFAULT false NOT NULL,
    displayed_name text NOT NULL,
    displayed_desc text NOT NULL,
    description text NOT NULL,
    displayed_gametype text NOT NULL,
    cert_required boolean DEFAULT false NOT NULL,
    xp_bonus smallint DEFAULT 0 NOT NULL,
    sort_order smallint,
    rotation_priority smallint DEFAULT 1 NOT NULL,
    skip_matchmaking boolean DEFAULT true NOT NULL,
    queueing_enabled boolean DEFAULT true NOT NULL,
    team_count smallint DEFAULT 1 NOT NULL,
    min_players_per_team smallint DEFAULT 1 NOT NULL,
    max_players_per_team smallint DEFAULT 5 NOT NULL,
    min_players_accept_per_team smallint DEFAULT 0 NOT NULL,
    challenge_enabled boolean DEFAULT false NOT NULL,
    challenge_min_players_per_team smallint DEFAULT 0 NOT NULL,
    challenge_max_players_per_team smallint DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT false NOT NULL,
    images text
);


ALTER TABLE webapi."ZoneSettings" OWNER TO tmwadmin;

--
-- Name: ZoneSettings_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

CREATE SEQUENCE webapi."ZoneSettings_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE webapi."ZoneSettings_id_seq" OWNER TO tmwadmin;

--
-- Name: ZoneSettings_id_seq; Type: SEQUENCE OWNED BY; Schema: webapi; Owner: tmwadmin
--

ALTER SEQUENCE webapi."ZoneSettings_id_seq" OWNED BY webapi."ZoneSettings".id;


--
-- Name: ZoneCertificates id; Type: DEFAULT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ZoneCertificates" ALTER COLUMN id SET DEFAULT nextval('webapi."ZoneCertificates_id_seq"'::regclass);


--
-- Name: ZoneDifficulty id; Type: DEFAULT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ZoneDifficulty" ALTER COLUMN id SET DEFAULT nextval('webapi."ZoneDifficulty_id_seq"'::regclass);


--
-- Name: ZoneSettings id; Type: DEFAULT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ZoneSettings" ALTER COLUMN id SET DEFAULT nextval('webapi."ZoneSettings_id_seq"'::regclass);


--
-- Name: Battleframes Character and Battleframe Type; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."Battleframes"
    ADD CONSTRAINT "Character and Battleframe Type" UNIQUE (character_guid, battleframe_sdb_id);


--
-- Name: ClientEvents ClientEvents_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ClientEvents"
    ADD CONSTRAINT "ClientEvents_pkey" PRIMARY KEY (id);


--
-- Name: Costs Costs_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."Costs"
    ADD CONSTRAINT "Costs_pkey" PRIMARY KEY (id);


--
-- Name: DeletionQueue DeletionQueue_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."DeletionQueue"
    ADD CONSTRAINT "DeletionQueue_pkey" PRIMARY KEY (character_guid);


--
-- Name: Purchases Purchases_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."Purchases"
    ADD CONSTRAINT "Purchases_pkey" PRIMARY KEY (account_id);


--
-- Name: ZoneCertificates ZoneCertificates_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ZoneCertificates"
    ADD CONSTRAINT "ZoneCertificates_pkey" PRIMARY KEY (id);


--
-- Name: ZoneDifficulty ZoneDifficulty_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ZoneDifficulty"
    ADD CONSTRAINT "ZoneDifficulty_pkey" PRIMARY KEY (id);


--
-- Name: ZoneSettings ZoneSettings_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ZoneSettings"
    ADD CONSTRAINT "ZoneSettings_pkey" PRIMARY KEY (id);


--
-- Name: Accounts accounts_pk; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."Accounts"
    ADD CONSTRAINT accounts_pk PRIMARY KEY (account_id);


--
-- Name: Battleframes battleframes_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."Battleframes"
    ADD CONSTRAINT battleframes_pkey PRIMARY KEY (id);


--
-- Name: Characters characters_pk; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."Characters"
    ADD CONSTRAINT characters_pk PRIMARY KEY (character_guid);


--
-- Name: LoginEvents login_events_pk; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."LoginEvents"
    ADD CONSTRAINT login_events_pk PRIMARY KEY (id);


--
-- Name: VipData vip_data_pk; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."VipData"
    ADD CONSTRAINT vip_data_pk PRIMARY KEY (account_id);


--
-- Name: accounts_account_id_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX accounts_account_id_uindex ON webapi."Accounts" USING btree (account_id);


--
-- Name: accounts_email_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX accounts_email_uindex ON webapi."Accounts" USING btree (email);


--
-- Name: accounts_uid_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX accounts_uid_uindex ON webapi."Accounts" USING btree (uid);


--
-- Name: characters_name_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX characters_name_uindex ON webapi."Characters" USING btree (name);


--
-- Name: login_events_id_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX login_events_id_uindex ON webapi."LoginEvents" USING btree (id);


--
-- Name: purchases_account_id_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX purchases_account_id_uindex ON webapi."Purchases" USING btree (account_id);


--
-- Name: purchases_purchase_id_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX purchases_purchase_id_uindex ON webapi."Purchases" USING btree (purchase_id);


--
-- Name: vip_data_account_id_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX vip_data_account_id_uindex ON webapi."VipData" USING btree (account_id);


--
-- Name: Battleframes Char ID; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."Battleframes"
    ADD CONSTRAINT "Char ID" FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) NOT VALID;


--
-- Name: VipData vipdata_accounts_account_id_fk; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."VipData"
    ADD CONSTRAINT vipdata_accounts_account_id_fk FOREIGN KEY (account_id) REFERENCES webapi."Accounts"(account_id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

