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
-- Name: army_rank_type; Type: TYPE; Schema: webapi; Owner: tmwadmin
--

CREATE TYPE webapi.army_rank_type AS (
	id integer,
	can_promote boolean,
	can_edit boolean,
	is_officer boolean,
	name text,
	can_invite boolean,
	can_kick boolean
);


ALTER TYPE webapi.army_rank_type OWNER TO tmwadmin;

--
-- Name: ApproveArmyApplicationsOrInvites(bigint, bigint, bigint[]); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi."ApproveArmyApplicationsOrInvites"(army_guid bigint, initiator_guid bigint, application_ids bigint[], OUT result boolean) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare
    payload              json;
    application_id       bigint;
    default_army_rank_id bigint;
    char_guid            bigint;
    army_name            text;
    char_name            text;
BEGIN
    result = FALSE;

    SELECT army_rank_id
    FROM webapi."ArmyRanks" ar
    WHERE is_default = true
      AND ar.army_guid = "ApproveArmyApplicationsOrInvites".army_guid
    INTO default_army_rank_id;

    FOREACH application_id IN ARRAY application_ids
        LOOP
            IF EXISTS (SELECT 1
                       FROM webapi."ArmyMembers" am
                                INNER JOIN webapi."ArmyApplications" aa ON am.character_guid = aa.character_guid
                       WHERE aa.id = application_id)
                THEN CONTINUE;
            END IF;

            INSERT INTO webapi."ArmyMembers" (army_guid, character_guid, army_rank_id)
            SELECT aa.army_guid,
                   character_guid,
                   default_army_rank_id
            FROM webapi."ArmyApplications" aa
            WHERE id = application_id
              AND aa.army_guid = "ApproveArmyApplicationsOrInvites".army_guid
            RETURNING character_guid INTO char_guid;

            DELETE
            FROM webapi."ArmyApplications" aa
            WHERE character_guid = char_guid;

            IF initiator_guid = char_guid THEN
                SELECT name
                FROM webapi."Characters"
                WHERE character_guid = char_guid
                INTO char_name;

                payload := json_build_object('ArmyGuid', army_guid, 'InitiatorName', char_name);
                PERFORM pg_notify('events', 'ArmyInviteApproved->' || payload::text);
            ELSE
                SELECT name
                FROM webapi."Armies" a
                WHERE a.army_guid = "ApproveArmyApplicationsOrInvites".army_guid
                INTO army_name;

                payload := json_build_object('CharacterGuid', char_guid, 'InitiatorName', army_name);
                PERFORM pg_notify('events', 'ArmyApplicationApproved->' || payload::text);
            END IF;
        END LOOP;

    result = TRUE;

EXCEPTION
    WHEN OTHERS THEN RAISE;
END
$$;


ALTER FUNCTION webapi."ApproveArmyApplicationsOrInvites"(army_guid bigint, initiator_guid bigint, application_ids bigint[], OUT result boolean) OWNER TO tmwadmin;

--
-- Name: CreateArmy(bigint, character varying, character varying, character varying, boolean, character varying, character varying, character varying); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi."CreateArmy"(commander_guid bigint, name character varying, website character varying, description character varying, is_recruiting boolean, playstyle character varying, region character varying, personality character varying, OUT army_id bigint, OUT error_text text) RETURNS record
    LANGUAGE plpgsql
    AS $$
declare
    c text;
    commander_rank_id bigint;
BEGIN

    -- Check if the name is already in use, case insensitive, the constraint will do this but want to avoid incrementing the sequence on fails
    IF (SELECT EXISTS (SELECT 1 FROM webapi."Armies" WHERE LOWER("Armies".name) = LOWER("CreateArmy".name))) THEN
        army_id = -1;
        error_text = 'ERR_INVALID_NAME';
        RETURN;
    END IF;

    INSERT INTO webapi."Armies" (
                                 army_guid, 
                                 name,
                                 website,
                                 description,
                                 is_recruiting,
                                 playstyle,
                                 region,
                                 personality,
                                 motd,
                                 login_message
    ) VALUES (
              webapi.create_entity_guid(252),
              name,
              website,
              description,
              is_recruiting,
              playstyle,
              region,
              personality,
              '',
              ''
    ) RETURNING army_guid INTO army_id;

    -- Create two required ranks (commander + default Soldier)
    INSERT INTO webapi."ArmyRanks" (army_guid, name, position, is_commander, can_invite, can_edit, can_promote, is_officer, can_mass_email, can_kick)
    VALUES (army_id, 'Commander', 1, true, true, true, true, true, true, true) RETURNING army_rank_id INTO commander_rank_id;

    INSERT INTO webapi."ArmyRanks" (army_guid, name, position, is_default)
    VALUES (army_id, 'Soldier', 2, true);

    INSERT INTO webapi."ArmyMembers" (army_guid, character_guid, army_rank_id)
    VALUES (army_id, commander_guid, commander_rank_id);

    EXCEPTION WHEN OTHERS THEN
        GET STACKED DIAGNOSTICS c := CONSTRAINT_NAME;
        IF c = 'Armies_name_uindex' THEN
            error_text = 'ERR_INVALID_NAME';
        ELSE
            error_text = 'ERR_UNKNOWN';
            RAISE;
        END IF;
END$$;


ALTER FUNCTION webapi."CreateArmy"(commander_guid bigint, name character varying, website character varying, description character varying, is_recruiting boolean, playstyle character varying, region character varying, personality character varying, OUT army_id bigint, OUT error_text text) OWNER TO tmwadmin;

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
-- Name: CreateNewCharacter(bigint, text, boolean, integer, integer, bytea); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
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


ALTER FUNCTION webapi."CreateNewCharacter"(account_id bigint, name text, is_dev boolean, voice_setid integer, gender integer, visuals bytea, OUT error_text text, OUT new_character_id bigint) OWNER TO tmwadmin;

--
-- Name: InviteToArmy(bigint, character varying, text, bigint); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi."InviteToArmy"(army_guid bigint, character_name character varying, message text, inviter_guid bigint, OUT result boolean, OUT error_text text) RETURNS record
    LANGUAGE plpgsql
    AS $$
declare
    c text;
BEGIN

    IF (SELECT EXISTS (SELECT 1 FROM webapi."ArmyMembers" am JOIN webapi."Characters" c USING (character_guid) 
                        WHERE c.name = character_name )) THEN
        result = FALSE;
--      "This player is already in an army"
        error_text = 'ERR_DUPLICATE_CHARACTER';
        RETURN;
    END IF;

    INSERT INTO webapi."ArmyApplications" (army_guid, character_guid, message, inviter_guid)
    VALUES ("InviteToArmy".army_guid,
            (SELECT c.character_guid FROM webapi."Characters" c WHERE c.name = character_name),
            "InviteToArmy".message,
            "InviteToArmy".inviter_guid);

    result = TRUE;

    EXCEPTION WHEN OTHERS THEN
        GET STACKED DIAGNOSTICS c := CONSTRAINT_NAME;
        IF c = 'ArmyApplications_army_guid_character_guid' THEN
            error_text = 'ERR_DUPLICATE_APPLICATION';
        ELSE
            error_text = 'ERR_UNKNOWN';
            RAISE;
        END IF;
END$$;


ALTER FUNCTION webapi."InviteToArmy"(army_guid bigint, character_name character varying, message text, inviter_guid bigint, OUT result boolean, OUT error_text text) OWNER TO tmwadmin;

--
-- Name: StepDownAsCommander(bigint, bigint, text); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi."StepDownAsCommander"(army_guid bigint, commander_guid bigint, new_commander_name text, OUT result boolean) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare
    current_commander_rank_id     bigint = -1;
    new_commander_guid            bigint = -1;
    new_commander_current_rank_id bigint = -1;
BEGIN
    result = FALSE;
    
    SELECT c.character_guid INTO new_commander_guid FROM webapi."Characters" c WHERE c.name = new_commander_name;

    SELECT ar.army_rank_id
    FROM webapi."ArmyRanks" ar
             JOIN webapi."ArmyMembers" am USING (army_rank_id)
    WHERE am.character_guid = commander_guid
      AND am.army_guid = "StepDownAsCommander".army_guid
    INTO current_commander_rank_id;

    SELECT ar.army_rank_id
    FROM webapi."ArmyRanks" ar
             JOIN webapi."ArmyMembers" am USING (army_rank_id)
    WHERE am.character_guid = new_commander_guid
      AND am.army_guid = "StepDownAsCommander".army_guid
    INTO new_commander_current_rank_id;

    IF NOT (SELECT EXISTS (SELECT 1
                           FROM webapi."ArmyMembers" am
                           WHERE am.character_guid = new_commander_guid
                             AND am.army_guid = "StepDownAsCommander".army_guid)) THEN
        result = FALSE;
        RETURN;
    END IF;

    UPDATE webapi."ArmyMembers" am
    SET army_rank_id = current_commander_rank_id
    WHERE character_guid = new_commander_guid
      AND am.army_guid = "StepDownAsCommander".army_guid;

    UPDATE webapi."ArmyMembers" am
    SET army_rank_id = new_commander_current_rank_id
    WHERE character_guid = commander_guid
      AND am.army_guid = "StepDownAsCommander".army_guid;

    result = TRUE;

EXCEPTION
    WHEN OTHERS THEN RAISE;
END
$$;


ALTER FUNCTION webapi."StepDownAsCommander"(army_guid bigint, commander_guid bigint, new_commander_name text, OUT result boolean) OWNER TO tmwadmin;

--
-- Name: UpdateArmyRanks(bigint, integer, text); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi."UpdateArmyRanks"(army_guid bigint, initiator_rank_position integer, ranks_json text, OUT result boolean) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    rank webapi.army_rank_type;
    ranks webapi.army_rank_type[];
BEGIN
    ranks := ARRAY(SELECT (id, can_promote, can_edit, is_officer, name, can_invite, can_kick) 
                   FROM json_populate_recordset(NULL::webapi.army_rank_type, ranks_json::json));

    FOREACH rank IN ARRAY ranks
        LOOP
            UPDATE webapi."ArmyRanks" ar
            SET name        = COALESCE(rank.name, name),
                can_edit    = rank.can_edit,
                can_invite  = rank.can_invite,
                can_kick    = rank.can_kick,
                is_officer  = rank.is_officer,
                can_promote = rank.can_promote,
                updated_at  = CURRENT_TIMESTAMP
            WHERE ar.army_guid = "UpdateArmyRanks".army_guid
              AND army_rank_id = rank.id
              AND position > initiator_rank_position;
        END LOOP;
    result = TRUE;
EXCEPTION
    WHEN OTHERS THEN
        result = FALSE;
        RAISE;
END;
$$;


ALTER FUNCTION webapi."UpdateArmyRanks"(army_guid bigint, initiator_rank_position integer, ranks_json text, OUT result boolean) OWNER TO tmwadmin;

--
-- Name: UpdateArmyRanksOrder(bigint, integer, bigint[]); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi."UpdateArmyRanksOrder"(army_guid bigint, initiator_rank_position integer, rank_ids bigint[], OUT result boolean) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
BEGIN
    WITH protected_ranks AS (
        SELECT army_rank_id, COUNT(*) OVER () as count_protected
        FROM webapi."ArmyRanks" ar
        WHERE ar.army_guid = "UpdateArmyRanksOrder".army_guid AND position <= initiator_rank_position
    ),
    target_order AS (
        SELECT r_id, ROW_NUMBER() OVER () as new_position
        FROM unnest(rank_ids) r_id
        WHERE r_id NOT IN (SELECT army_rank_id FROM protected_ranks)
    )
    UPDATE webapi."ArmyRanks" ar2 
    SET position   = tor.new_position + pr.count_protected,
        updated_at = CURRENT_TIMESTAMP
    FROM target_order tor, (SELECT count_protected FROM protected_ranks LIMIT 1) pr
    WHERE ar2.army_rank_id = tor.r_id AND ar2.army_guid = "UpdateArmyRanksOrder".army_guid;

    result := TRUE;
EXCEPTION
    WHEN OTHERS THEN
        result := FALSE;
        RAISE;
END;
$$;


ALTER FUNCTION webapi."UpdateArmyRanksOrder"(army_guid bigint, initiator_rank_position integer, rank_ids bigint[], OUT result boolean) OWNER TO tmwadmin;

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


--
-- Name: notify_on_army_application_events(); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi.notify_on_army_application_events() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    payload json;
    armyName text;
    initiatorName text;
    armyMemberGuids bigint[];
    armyGuid bigint := (CASE WHEN TG_OP = 'DELETE' THEN OLD.army_guid ELSE NEW.army_guid END);
    inviterGuid bigint := NEW.inviter_guid;
BEGIN
    SELECT ARRAY(
       SELECT character_guid
       FROM webapi."ArmyMembers" am
                INNER JOIN webapi."ArmyRanks" ar on ar.army_rank_id = am.army_rank_id
       WHERE am.army_guid = armyGuid
         AND ar.can_invite = TRUE)
    INTO armyMemberGuids;

    payload := json_build_object('ArmyMemberGuids', armyMemberGuids);
    PERFORM pg_notify('events', 'ArmyApplicationsUpdated->' || payload::text);

    IF TG_OP = 'INSERT' THEN
        IF inviterGuid IS NOT NULL THEN
            SELECT name
            INTO armyName
            FROM webapi."Armies"
            WHERE army_guid = armyGuid;

            SELECT name
            INTO initiatorName
            FROM webapi."Characters"
            WHERE character_guid = inviterGuid;

            payload := json_build_object(
                'ArmyGuid', armyGuid,
                'ArmyName', armyName,
                'CharacterGuid', NEW.character_guid,
                'Id', NEW.id,
                'Message', NEW.message,
                'InitiatorName', initiatorName
            );
            PERFORM pg_notify('events', 'ArmyInviteReceived->' || payload::text);
        ELSE
            SELECT name
            INTO initiatorName
            FROM webapi."Characters"
            WHERE character_guid = NEW.character_guid;

            payload := json_build_object('ArmyMemberGuids', armyMemberGuids, 'InitiatorName', initiatorName);
            PERFORM pg_notify('events', 'ArmyApplicationReceived->' || payload::text);
        END IF;
    ELSIF TG_OP = 'DELETE' THEN
        IF (SELECT EXISTS (SELECT 1 FROM webapi."ArmyMembers" am
                                JOIN webapi."Characters" c USING (character_guid)
                           WHERE c.character_guid = OLD.character_guid
                             AND am.army_guid = OLD.army_guid))
        THEN
            RETURN NULL;
        END IF;

        IF OLD.inviter_guid IS NULL THEN
            SELECT name
            INTO armyName
            FROM webapi."Armies"
            WHERE army_guid = armyGuid;

            SELECT name
            INTO initiatorName
            FROM webapi."Characters"
            WHERE character_guid = OLD.character_guid;

            payload := json_build_object(
                'CharacterGuid', OLD.character_guid,
                'InitiatorName', armyName
            );
            PERFORM pg_notify('events', 'ArmyApplicationRejected->' || payload::text);
        ELSE
            SELECT name
            INTO initiatorName
            FROM webapi."Characters"
            WHERE character_guid = OLD.character_guid;

            payload := json_build_object(
                'ArmyMemberGuids', armyMemberGuids,
                'InitiatorName', initiatorName
            );
            PERFORM pg_notify('events', 'ArmyInviteRejected->' || payload::text);
        END IF;
    END IF;

    RETURN NULL;
END;
$$;


ALTER FUNCTION webapi.notify_on_army_application_events() OWNER TO tmwadmin;

--
-- Name: notify_on_army_events(); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi.notify_on_army_events() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    payload json;
BEGIN
    IF (OLD.tag IS NULL AND NEW.tag IS NOT NULL) OR NEW.tag <> OLD.tag THEN
        payload := json_build_object('ArmyGuid', NEW.army_guid, 'ArmyTag', NEW.tag);
        PERFORM pg_notify('events', 'ArmyTagUpdated->' || payload::text);
    END IF;

    payload := json_build_object('ArmyGuid', NEW.army_guid);
    PERFORM pg_notify('events', 'ArmyInfoUpdated->' || payload::text);

    RETURN NULL;
END;
$$;


ALTER FUNCTION webapi.notify_on_army_events() OWNER TO tmwadmin;

--
-- Name: notify_on_army_member_events(); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi.notify_on_army_member_events() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    payload json;
    isOfficer boolean;
    armyTag text;
BEGIN
    IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
        SELECT is_officer
        INTO isOfficer
        FROM webapi."ArmyRanks"
        WHERE army_rank_id = NEW.army_rank_id;
        
        SELECT tag
        INTO armyTag
        FROM webapi."Armies"
        WHERE army_guid = NEW.army_guid;

        payload := json_build_object(
            'ArmyGuid', NEW.army_guid,
            'CharacterGuid', NEW.character_guid,
            'IsOfficer', isOfficer,
            'ArmyTag', COALESCE(armyTag, '')
        );
        PERFORM pg_notify('events', 'ArmyIdChanged->' || payload::text);
        
        PERFORM pg_notify('events', 'ArmyMembersUpdated->' || json_build_object('ArmyGuid', NEW.army_guid)::text);
    ELSE
        payload := json_build_object('ArmyGuid', 0, 'CharacterGuid', OLD.character_guid, 'IsOfficer', false, 'ArmyTag', '');
        PERFORM pg_notify('events', 'ArmyIdChanged->' || payload::text);
        
        PERFORM pg_notify('events', 'ArmyMembersUpdated->' || json_build_object('ArmyGuid', OLD.army_guid)::text);
        
        IF (SELECT count(*) FROM webapi."ArmyMembers" WHERE army_guid = OLD.army_guid) < 3 THEN
            UPDATE webapi."Armies" SET tag = NULL WHERE army_guid = OLD.army_guid;
            
            PERFORM pg_notify('events', 'ArmyTagUpdated->' || json_build_object('ArmyGuid', OLD.army_guid, 'ArmyTag', '')::text);
        END IF;
    END IF;
    
    RETURN NULL;
END;
$$;


ALTER FUNCTION webapi.notify_on_army_member_events() OWNER TO tmwadmin;

--
-- Name: notify_on_army_rank_events(); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi.notify_on_army_rank_events() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    payload json;
    armyGuid bigint := (CASE WHEN TG_OP = 'DELETE' THEN OLD.army_guid ELSE NEW.army_guid END);
BEGIN
    payload := json_build_object('ArmyGuid', armyGuid);
    PERFORM pg_notify('events', 'ArmyRanksUpdated->' || payload::text);

    RETURN NULL;
END;
$$;


ALTER FUNCTION webapi.notify_on_army_rank_events() OWNER TO tmwadmin;

--
-- Name: notify_on_character_events(); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi.notify_on_character_events() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    payload json;
BEGIN
    IF OLD.gender != NEW.gender OR OLD.race != NEW.race OR OLD.visuals != NEW.visuals THEN
        payload := json_build_object('CharacterGuid', NEW.character_guid);
        PERFORM pg_notify('events', 'CharacterVisualsUpdated->' || payload::text);
    END IF;
    RETURN NEW;
END;
$$;


ALTER FUNCTION webapi.notify_on_character_events() OWNER TO tmwadmin;

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
-- Name: Armies; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."Armies" (
    army_guid bigint NOT NULL,
    is_recruiting boolean DEFAULT false NOT NULL,
    name character varying(32) NOT NULL,
    personality character varying NOT NULL,
    playstyle character varying NOT NULL,
    tag character varying(6),
    region character varying(6) NOT NULL,
    description character varying(2048) NOT NULL,
    motd text NOT NULL,
    website character varying(255),
    login_message text NOT NULL,
    established_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE webapi."Armies" OWNER TO tmwadmin;

--
-- Name: COLUMN "Armies".personality; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."Armies".personality IS 'Casual,Moderate,Hardcore';


--
-- Name: COLUMN "Armies".playstyle; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."Armies".playstyle IS 'pve,pvp,pvx';


--
-- Name: COLUMN "Armies".region; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."Armies".region IS 'NA,Europe,AUS/NZ,China,All';


--
-- Name: ArmyApplications; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."ArmyApplications" (
    army_guid bigint NOT NULL,
    character_guid bigint NOT NULL,
    message text NOT NULL,
    id bigint NOT NULL,
    inviter_guid bigint
);


ALTER TABLE webapi."ArmyApplications" OWNER TO tmwadmin;

--
-- Name: ArmyApplications_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

CREATE SEQUENCE webapi."ArmyApplications_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE webapi."ArmyApplications_id_seq" OWNER TO tmwadmin;

--
-- Name: ArmyApplications_id_seq; Type: SEQUENCE OWNED BY; Schema: webapi; Owner: tmwadmin
--

ALTER SEQUENCE webapi."ArmyApplications_id_seq" OWNED BY webapi."ArmyApplications".id;


--
-- Name: ArmyApplications_id_seq1; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE webapi."ArmyApplications" ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME webapi."ArmyApplications_id_seq1"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: ArmyMembers; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."ArmyMembers" (
    army_guid bigint NOT NULL,
    character_guid bigint NOT NULL,
    army_rank_id bigint NOT NULL,
    public_note character varying(255)
);


ALTER TABLE webapi."ArmyMembers" OWNER TO tmwadmin;

--
-- Name: ArmyRanks; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."ArmyRanks" (
    army_rank_id bigint NOT NULL,
    army_guid bigint NOT NULL,
    name character varying(255) NOT NULL,
    "position" smallint NOT NULL,
    is_commander boolean DEFAULT false NOT NULL,
    can_invite boolean DEFAULT false NOT NULL,
    can_edit boolean DEFAULT false NOT NULL,
    can_promote boolean DEFAULT false NOT NULL,
    is_officer boolean DEFAULT false NOT NULL,
    is_default boolean DEFAULT false NOT NULL,
    can_mass_email boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp with time zone,
    can_kick boolean DEFAULT false NOT NULL
);


ALTER TABLE webapi."ArmyRanks" OWNER TO tmwadmin;

--
-- Name: ArmyRanks_army_rank_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE webapi."ArmyRanks" ALTER COLUMN army_rank_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME webapi."ArmyRanks_army_rank_id_seq"
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
    current_battleframe_guid bigint,
    last_zone_id integer,
    last_outpost_id integer
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
-- Name: LeaderboardEntries; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."LeaderboardEntries" (
    leaderboard_id integer NOT NULL,
    character_guid bigint NOT NULL,
    value integer NOT NULL
);


ALTER TABLE webapi."LeaderboardEntries" OWNER TO tmwadmin;

--
-- Name: Leaderboards; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."Leaderboards" (
    id integer NOT NULL,
    name character varying(255) NOT NULL,
    type smallint NOT NULL,
    "order" smallint NOT NULL
);


ALTER TABLE webapi."Leaderboards" OWNER TO tmwadmin;

--
-- Name: COLUMN "Leaderboards".type; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."Leaderboards".type IS '0=resource,1=time';


--
-- Name: COLUMN "Leaderboards"."order"; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."Leaderboards"."order" IS '0=asc,1=desc';


--
-- Name: Leaderboards_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

CREATE SEQUENCE webapi."Leaderboards_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE webapi."Leaderboards_id_seq" OWNER TO tmwadmin;

--
-- Name: Leaderboards_id_seq; Type: SEQUENCE OWNED BY; Schema: webapi; Owner: tmwadmin
--

ALTER SEQUENCE webapi."Leaderboards_id_seq" OWNED BY webapi."Leaderboards".id;


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
-- Name: Leaderboards id; Type: DEFAULT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."Leaderboards" ALTER COLUMN id SET DEFAULT nextval('webapi."Leaderboards_id_seq"'::regclass);


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
-- Name: Armies Armies_army_guid; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."Armies"
    ADD CONSTRAINT "Armies_army_guid" PRIMARY KEY (army_guid);


--
-- Name: ArmyApplications ArmyApplications_army_guid_character_guid; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ArmyApplications"
    ADD CONSTRAINT "ArmyApplications_army_guid_character_guid" UNIQUE (army_guid, character_guid);


--
-- Name: ArmyApplications ArmyApplications_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ArmyApplications"
    ADD CONSTRAINT "ArmyApplications_pkey" PRIMARY KEY (id);


--
-- Name: ArmyRanks ArmyRanks_army_rank_guid; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ArmyRanks"
    ADD CONSTRAINT "ArmyRanks_army_rank_guid" PRIMARY KEY (army_rank_id);


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
-- Name: LeaderboardEntries LeaderboardEntries_leaderboard_id_character_guid; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."LeaderboardEntries"
    ADD CONSTRAINT "LeaderboardEntries_leaderboard_id_character_guid" UNIQUE (leaderboard_id, character_guid);


--
-- Name: Leaderboards Leaderboards_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."Leaderboards"
    ADD CONSTRAINT "Leaderboards_pkey" PRIMARY KEY (id);


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
-- Name: Armies_name_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX "Armies_name_uindex" ON webapi."Armies" USING btree (lower((name)::text));


--
-- Name: LeaderboardEntries_leaderboard_id_value; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE INDEX "LeaderboardEntries_leaderboard_id_value" ON webapi."LeaderboardEntries" USING btree (leaderboard_id, value);


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
-- Name: ArmyApplications army_application_events_trigger; Type: TRIGGER; Schema: webapi; Owner: tmwadmin
--

CREATE TRIGGER army_application_events_trigger AFTER INSERT OR DELETE OR UPDATE ON webapi."ArmyApplications" FOR EACH ROW EXECUTE FUNCTION webapi.notify_on_army_application_events();


--
-- Name: Armies army_events_trigger; Type: TRIGGER; Schema: webapi; Owner: tmwadmin
--

CREATE TRIGGER army_events_trigger AFTER UPDATE ON webapi."Armies" FOR EACH ROW EXECUTE FUNCTION webapi.notify_on_army_events();


--
-- Name: ArmyMembers army_member_events_trigger; Type: TRIGGER; Schema: webapi; Owner: tmwadmin
--

CREATE TRIGGER army_member_events_trigger AFTER INSERT OR DELETE OR UPDATE ON webapi."ArmyMembers" FOR EACH ROW EXECUTE FUNCTION webapi.notify_on_army_member_events();


--
-- Name: ArmyRanks army_rank_events_trigger; Type: TRIGGER; Schema: webapi; Owner: tmwadmin
--

CREATE TRIGGER army_rank_events_trigger AFTER INSERT OR DELETE OR UPDATE ON webapi."ArmyRanks" FOR EACH ROW EXECUTE FUNCTION webapi.notify_on_army_rank_events();


--
-- Name: Characters character_events_trigger; Type: TRIGGER; Schema: webapi; Owner: tmwadmin
--

CREATE TRIGGER character_events_trigger AFTER UPDATE ON webapi."Characters" FOR EACH ROW EXECUTE FUNCTION webapi.notify_on_character_events();


--
-- Name: ArmyApplications ArmyApplications_army_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ArmyApplications"
    ADD CONSTRAINT "ArmyApplications_army_guid_fkey" FOREIGN KEY (army_guid) REFERENCES webapi."Armies"(army_guid) ON DELETE CASCADE;


--
-- Name: ArmyApplications ArmyApplications_character_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ArmyApplications"
    ADD CONSTRAINT "ArmyApplications_character_guid_fkey" FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid);


--
-- Name: ArmyApplications ArmyApplications_inviter_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ArmyApplications"
    ADD CONSTRAINT "ArmyApplications_inviter_guid_fkey" FOREIGN KEY (inviter_guid) REFERENCES webapi."Characters"(character_guid);


--
-- Name: ArmyMembers ArmyMembers_army_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ArmyMembers"
    ADD CONSTRAINT "ArmyMembers_army_guid_fkey" FOREIGN KEY (army_guid) REFERENCES webapi."Armies"(army_guid) ON DELETE CASCADE;


--
-- Name: ArmyMembers ArmyMembers_army_rank_id_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ArmyMembers"
    ADD CONSTRAINT "ArmyMembers_army_rank_id_fkey" FOREIGN KEY (army_rank_id) REFERENCES webapi."ArmyRanks"(army_rank_id);


--
-- Name: ArmyRanks ArmyRanks_army_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ArmyRanks"
    ADD CONSTRAINT "ArmyRanks_army_guid_fkey" FOREIGN KEY (army_guid) REFERENCES webapi."Armies"(army_guid) ON DELETE CASCADE;


--
-- Name: Battleframes Char ID; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."Battleframes"
    ADD CONSTRAINT "Char ID" FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) NOT VALID;


--
-- Name: LeaderboardEntries LeaderboardEntries_character_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."LeaderboardEntries"
    ADD CONSTRAINT "LeaderboardEntries_character_guid_fkey" FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE;


--
-- Name: LeaderboardEntries LeaderboardEntries_leaderboard_id_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."LeaderboardEntries"
    ADD CONSTRAINT "LeaderboardEntries_leaderboard_id_fkey" FOREIGN KEY (leaderboard_id) REFERENCES webapi."Leaderboards"(id) ON DELETE CASCADE;


--
-- Name: ArmyMembers character_guid; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ArmyMembers"
    ADD CONSTRAINT character_guid FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid);


--
-- Name: VipData vipdata_accounts_account_id_fk; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."VipData"
    ADD CONSTRAINT vipdata_accounts_account_id_fk FOREIGN KEY (account_id) REFERENCES webapi."Accounts"(account_id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

