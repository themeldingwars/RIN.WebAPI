--
-- PostgreSQL database dump
--

\restrict BTbasBS3VXkGFnU2xx53bKM25c9zv0oK9FdpuVbGmQap1NLDI6d5nTkc8gviWmv

-- Dumped from database version 18.6
-- Dumped by pg_dump version 18.6

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
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
-- Name: CreateNewCharacter(bigint, text, boolean, integer, integer, bytea, integer); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi."CreateNewCharacter"(account_id bigint, name text, is_dev boolean, voice_setid integer, gender integer, visuals bytea, template_id integer DEFAULT 0, OUT error_text text, OUT new_character_id bigint) RETURNS record
    LANGUAGE plpgsql
    AS $$
declare
    c text;
    v_template_id integer;
BEGIN

    -- Check if the name isn't used
    IF (SELECT exists(SELECT 1 FROM webapi."Characters" WHERE webapi."Characters".name = "CreateNewCharacter".name)) THEN
        new_character_id = -1;
        error_text = 'ERR_NAME_IN_USE';
        RETURN;
    END IF;

    v_template_id := coalesce(nullif("CreateNewCharacter".template_id, 0),
                              (SELECT t.template_id FROM webapi."NewCharacterTemplates" t WHERE t.is_default LIMIT 1));

    IF v_template_id IS NULL THEN
        new_character_id = -1;
        error_text = 'ERR_NO_CHARACTER_TEMPLATE';
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

    PERFORM webapi.seed_character_inventory(new_character_id, v_template_id);

    error_text = '';

END
$$;


ALTER FUNCTION webapi."CreateNewCharacter"(account_id bigint, name text, is_dev boolean, voice_setid integer, gender integer, visuals bytea, template_id integer, OUT error_text text, OUT new_character_id bigint) OWNER TO tmwadmin;

--
-- Name: InvalidateLoginTicketOnPasswordChange(); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi."InvalidateLoginTicketOnPasswordChange"() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.login_ticket            := NULL;
    NEW.login_ticket_hash       := NULL;
    NEW.login_ticket_created_at := NULL;
    RETURN NEW;
END
$$;


ALTER FUNCTION webapi."InvalidateLoginTicketOnPasswordChange"() OWNER TO tmwadmin;

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
-- Name: build_template_from_char_create(text, integer, integer[], boolean); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi.build_template_from_char_create(p_template_name text, p_battleframe_level integer DEFAULT 1, p_frames integer[] DEFAULT NULL::integer[], p_allow_pvp_fallback boolean DEFAULT false) RETURNS integer
    LANGUAGE plpgsql
    AS $$
declare
    v_template_id integer;
    v_bandwidth   integer;
BEGIN
    IF p_battleframe_level < 1 THEN
        RAISE EXCEPTION 'build_template_from_char_create: battleframe level must be at least 1';
    END IF;

    -- What the frame's level buys in perk points. PIN recomputes this at runtime; the template only
    -- needs a starting value that matches the level the template is built for.
    v_bandwidth := coalesce((SELECT sum(fpl.perk_points_gain)
                             FROM sdb."dbitems::FrameProgressionLevel" fpl
                             WHERE fpl.level <= p_battleframe_level), 0);

    INSERT INTO webapi."NewCharacterTemplates" (name, is_default, description)
    VALUES (p_template_name, false, 'Built from sdb dbcharacter::CharCreateLoadout (default_pve_module)')
    ON CONFLICT (name) DO UPDATE
        SET description = EXCLUDED.description
    RETURNING template_id INTO v_template_id;

    DELETE FROM webapi."NewCharacterTemplateLoadoutPerks"  WHERE template_id = v_template_id;
    DELETE FROM webapi."NewCharacterTemplateLoadoutSlots" WHERE template_id = v_template_id;
    DELETE FROM webapi."NewCharacterTemplateItemModules"  WHERE template_id = v_template_id;
    DELETE FROM webapi."NewCharacterTemplateLoadouts"     WHERE template_id = v_template_id;
    DELETE FROM webapi."NewCharacterTemplateResources"    WHERE template_id = v_template_id;
    DELETE FROM webapi."NewCharacterTemplateItems"        WHERE template_id = v_template_id;
    DELETE FROM webapi."NewCharacterTemplateBattleframes" WHERE template_id = v_template_id;

    -- One character-create loadout per frame: prefer non-dev, non-experimental, the "- Player" row,
    -- then the starting loadout, then the lowest id.
    DROP TABLE IF EXISTS _build_frames;
    CREATE TEMPORARY TABLE _build_frames ON COMMIT DROP AS
    SELECT DISTINCT ON (c.frame_id)
           c.frame_id,
           c.id   AS ccl_id,
           c.name AS loadout_name
    FROM sdb."dbcharacter::CharCreateLoadout" c
    WHERE (p_frames IS NULL AND c.name LIKE '%- Player') OR c.frame_id = ANY (coalesce(p_frames, '{}'))
    ORDER BY c.frame_id,
             c.is_dev,
             c.is_experimental,
             (c.name LIKE '%- Player') DESC,
             c.is_starting_loadout DESC,
             c.id;

    INSERT INTO webapi."NewCharacterTemplateBattleframes" (template_id, battleframe_sdb_id, level, bandwidth)
    SELECT v_template_id, frame_id, p_battleframe_level, v_bandwidth
    FROM _build_frames;

    INSERT INTO webapi."NewCharacterTemplateLoadouts" (template_id, template_loadout_id, chassis_id, loadout_name, loadout_type)
    SELECT v_template_id, row_number() OVER (ORDER BY frame_id), frame_id, loadout_name, 'battleframe'
    FROM _build_frames;

    -- One item instance per filled slot that the template's battleframe level can actually use.
    -- slot_type is a LoadoutSlot id; LoadoutSlot.level_requirement is when the slot unlocks.
    DROP TABLE IF EXISTS _build_slots;
    CREATE TEMPORARY TABLE _build_slots ON COMMIT DROP AS
    SELECT v_template_id AS template_id,
           row_number() OVER (ORDER BY f.frame_id, s.slot_type) AS seq,
           f.frame_id,
           s.slot_type,
           case when p_allow_pvp_fallback
                then coalesce(nullif(s.default_pve_module, 0), nullif(s.default_pvp_module, 0))
                else nullif(s.default_pve_module, 0)
           end AS sdb_id
    FROM _build_frames f
    JOIN sdb."dbcharacter::CharCreateLoadoutSlots" s ON s.loadout_id = f.ccl_id
    JOIN sdb."dbitems::LoadoutSlot"                  ls ON ls.id = s.slot_type
    WHERE ls.level_requirement <= p_battleframe_level
      AND case when p_allow_pvp_fallback
               then coalesce(nullif(s.default_pve_module, 0), nullif(s.default_pvp_module, 0))
               else nullif(s.default_pve_module, 0)
          end IS NOT NULL;

    INSERT INTO webapi."NewCharacterTemplateItems" (template_id, template_item_id, sdb_id)
    SELECT template_id, seq, sdb_id
    FROM _build_slots;

    INSERT INTO webapi."NewCharacterTemplateLoadoutSlots" (template_id, template_loadout_id, config_id, slot_index, template_item_id)
    SELECT v_template_id, l.template_loadout_id, 0, b.slot_type, b.seq
    FROM _build_slots b
    JOIN webapi."NewCharacterTemplateLoadouts" l
      ON l.template_id = v_template_id AND l.chassis_id = b.frame_id;

    -- No perks: sdb dbcharacter::CharCreateLoadoutPerks lists what a frame unlocks at each level, not
    -- what is slotted. Which of those the player picks is not something a template can decide.

    DROP TABLE IF EXISTS _build_slots;
    DROP TABLE IF EXISTS _build_frames;

    RETURN v_template_id;
END
$$;


ALTER FUNCTION webapi.build_template_from_char_create(p_template_name text, p_battleframe_level integer, p_frames integer[], p_allow_pvp_fallback boolean) OWNER TO tmwadmin;

--
-- Name: FUNCTION build_template_from_char_create(p_template_name text, p_battleframe_level integer, p_frames integer[], p_allow_pvp_fallback boolean); Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON FUNCTION webapi.build_template_from_char_create(p_template_name text, p_battleframe_level integer, p_frames integer[], p_allow_pvp_fallback boolean) IS 'Builds a seeding template from sdb dbcharacter::CharCreateLoadout. Slots above LoadoutSlot.level_requirement are skipped, as are slots with a zero default_pve_module. Bandwidth is set from cumulative dbitems::FrameProgressionLevel.perk_points_gain. No perks are seeded: CharCreateLoadoutPerks is an unlock list, not an equipped set.';


--
-- Name: create_chassis_item_for_loadout(); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi.create_chassis_item_for_loadout() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO webapi."CharacterItems" (item_guid, character_guid, sdb_id, sub_inventory, loadout_id)
    VALUES (webapi.create_entity_guid(253), NEW.character_guid, NEW.chassis_id, 4, NEW.loadout_id)
    ON CONFLICT DO NOTHING;

    RETURN NEW;
END
$$;


ALTER FUNCTION webapi.create_chassis_item_for_loadout() OWNER TO tmwadmin;

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

--
-- Name: notify_on_unlock_events(); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi.notify_on_unlock_events() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    payload json;
    char_guid bigint;
BEGIN
    IF TG_OP = 'DELETE' THEN
        char_guid = OLD.character_guid;
    ELSE
        char_guid = NEW.character_guid;
    END IF;

    payload := json_build_object('CharacterGuid', char_guid);
    PERFORM pg_notify('events', 'UnlocksUpdated->' || payload::text);
    RETURN NULL;
END;
$$;


ALTER FUNCTION webapi.notify_on_unlock_events() OWNER TO tmwadmin;

--
-- Name: seed_character_inventory(bigint, integer); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi.seed_character_inventory(char_guid bigint, p_template_id integer DEFAULT 0) RETURNS void
    LANGUAGE plpgsql
    AS $$
declare
    v_template_id integer;
BEGIN
    -- 0 means "whatever the default template is"
    v_template_id := coalesce(nullif(p_template_id, 0),
                              (SELECT t.template_id FROM webapi."NewCharacterTemplates" t WHERE t.is_default LIMIT 1));

    IF v_template_id IS NULL THEN
        RAISE EXCEPTION 'seed_character_inventory: no template % and no default template', p_template_id;
    END IF;

    IF EXISTS (SELECT 1 FROM webapi."CharacterItems" WHERE character_guid = char_guid) THEN
        RETURN;
    END IF;

    DROP TABLE IF EXISTS _seed_item_map;
    DROP TABLE IF EXISTS _seed_loadout_map;

    CREATE TEMPORARY TABLE _seed_item_map ON COMMIT DROP AS
    SELECT t.template_id, t.template_item_id, webapi.create_entity_guid(253) AS item_guid
    FROM webapi."NewCharacterTemplateItems" t
    WHERE t.template_id = v_template_id;

    INSERT INTO webapi."CharacterItems" (item_guid, character_guid, sdb_id, sub_inventory, dynamic_flags, durability)
    SELECT m.item_guid, char_guid, t.sdb_id, t.sub_inventory, t.dynamic_flags, t.durability
    FROM webapi."NewCharacterTemplateItems" t
    JOIN _seed_item_map m USING (template_id, template_item_id);

    INSERT INTO webapi."CharacterItemModules" (item_guid, slot_index, module_sdb_id)
    SELECT m.item_guid, tm.slot_index, tm.module_sdb_id
    FROM webapi."NewCharacterTemplateItemModules" tm
    JOIN _seed_item_map m USING (template_id, template_item_id);

    INSERT INTO webapi."CharacterResources" (character_guid, sdb_id, quantity, sub_inventory)
    SELECT char_guid, sdb_id, quantity, sub_inventory
    FROM webapi."NewCharacterTemplateResources"
    WHERE template_id = v_template_id;

    CREATE TEMPORARY TABLE _seed_loadout_map ON COMMIT DROP AS
    SELECT t.template_id, t.template_loadout_id, nextval('webapi."LoadoutId_Seq"')::integer AS loadout_id
    FROM webapi."NewCharacterTemplateLoadouts" t
    WHERE t.template_id = v_template_id;

    INSERT INTO webapi."CharacterLoadouts" (loadout_id, character_guid, chassis_id, loadout_name, loadout_type)
    SELECT lm.loadout_id, char_guid, t.chassis_id, t.loadout_name, t.loadout_type
    FROM webapi."NewCharacterTemplateLoadouts" t
    JOIN _seed_loadout_map lm USING (template_id, template_loadout_id);

    INSERT INTO webapi."CharacterLoadoutSlots" (loadout_id, config_id, slot_index, item_guid)
    SELECT lm.loadout_id, s.config_id, s.slot_index, im.item_guid
    FROM webapi."NewCharacterTemplateLoadoutSlots" s
    JOIN _seed_loadout_map lm USING (template_id, template_loadout_id)
    JOIN _seed_item_map im USING (template_id, template_item_id);

    INSERT INTO webapi."CharacterLoadoutPerks" (loadout_id, perk_item_sdb_id, level)
    SELECT lm.loadout_id, p.perk_item_sdb_id, p.level
    FROM webapi."NewCharacterTemplateLoadoutPerks" p
    JOIN _seed_loadout_map lm USING (template_id, template_loadout_id);

    -- One progression row per chassis. Frames the character already has keep their progression.
    INSERT INTO webapi."Battleframes" (id,
                                       character_guid,
                                       battleframe_sdb_id,
                                       visuals,
                                       hidden,
                                       level,
                                       current_xp,
                                       lifetime_xp,
                                       elite_level,
                                       current_elite_xp,
                                       lifetime_elite_xp,
                                       elite_points,
                                       bandwidth)
    SELECT webapi.create_entity_guid(253),
           char_guid,
           b.battleframe_sdb_id,
           b.visuals,
           b.hidden,
           b.level,
           0,
           0,
           b.elite_level,
           0,
           0,
           b.elite_points,
           b.bandwidth
    FROM webapi."NewCharacterTemplateBattleframes" b
    WHERE b.template_id = v_template_id
    ON CONFLICT (character_guid, battleframe_sdb_id) DO NOTHING;
END
$$;


ALTER FUNCTION webapi.seed_character_inventory(char_guid bigint, p_template_id integer) OWNER TO tmwadmin;

--
-- Name: snapshot_inventory_as_template(bigint, integer); Type: FUNCTION; Schema: webapi; Owner: tmwadmin
--

CREATE FUNCTION webapi.snapshot_inventory_as_template(char_guid bigint, p_template_id integer) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM webapi."NewCharacterTemplates" t WHERE t.template_id = p_template_id) THEN
        RAISE EXCEPTION 'snapshot_inventory_as_template: no template %', p_template_id;
    END IF;

    DELETE FROM webapi."NewCharacterTemplateLoadoutPerks"  WHERE template_id = p_template_id;
    DELETE FROM webapi."NewCharacterTemplateLoadoutSlots" WHERE template_id = p_template_id;
    DELETE FROM webapi."NewCharacterTemplateItemModules"  WHERE template_id = p_template_id;
    DELETE FROM webapi."NewCharacterTemplateLoadouts"     WHERE template_id = p_template_id;
    DELETE FROM webapi."NewCharacterTemplateResources"    WHERE template_id = p_template_id;
    DELETE FROM webapi."NewCharacterTemplateItems"        WHERE template_id = p_template_id;
    DELETE FROM webapi."NewCharacterTemplateBattleframes" WHERE template_id = p_template_id;

    -- Chassis items are excluded: they belong to the loadout, and seeding makes them from
    -- NewCharacterTemplateLoadouts. Keeping them here would seed each chassis twice.
    INSERT INTO webapi."NewCharacterTemplateItems" (template_id, template_item_id, sdb_id, sub_inventory, dynamic_flags, durability)
    SELECT p_template_id, row_number() OVER (ORDER BY item_guid), sdb_id, sub_inventory, dynamic_flags, durability
    FROM webapi."CharacterItems"
    WHERE character_guid = char_guid
      AND loadout_id IS NULL;

    INSERT INTO webapi."NewCharacterTemplateItemModules" (template_id, template_item_id, slot_index, module_sdb_id)
    SELECT p_template_id, im.template_item_id, m.slot_index, m.module_sdb_id
    FROM webapi."CharacterItemModules" m
    JOIN (SELECT item_guid, row_number() OVER (ORDER BY item_guid) AS template_item_id
          FROM webapi."CharacterItems" WHERE character_guid = char_guid AND loadout_id IS NULL) im USING (item_guid);

    INSERT INTO webapi."NewCharacterTemplateResources" (template_id, sdb_id, quantity, sub_inventory)
    SELECT p_template_id, sdb_id, quantity, sub_inventory
    FROM webapi."CharacterResources"
    WHERE character_guid = char_guid;

    INSERT INTO webapi."NewCharacterTemplateLoadouts" (template_id, template_loadout_id, chassis_id, loadout_name, loadout_type)
    SELECT p_template_id, row_number() OVER (ORDER BY loadout_id), chassis_id, loadout_name, loadout_type
    FROM webapi."CharacterLoadouts"
    WHERE character_guid = char_guid;

    INSERT INTO webapi."NewCharacterTemplateLoadoutSlots" (template_id, template_loadout_id, config_id, slot_index, template_item_id)
    SELECT p_template_id, lm.template_loadout_id, s.config_id, s.slot_index, im.template_item_id
    FROM webapi."CharacterLoadoutSlots" s
    JOIN (SELECT loadout_id, row_number() OVER (ORDER BY loadout_id) AS template_loadout_id
          FROM webapi."CharacterLoadouts" WHERE character_guid = char_guid) lm USING (loadout_id)
    JOIN (SELECT item_guid, row_number() OVER (ORDER BY item_guid) AS template_item_id
          FROM webapi."CharacterItems" WHERE character_guid = char_guid AND loadout_id IS NULL) im USING (item_guid);

    INSERT INTO webapi."NewCharacterTemplateLoadoutPerks" (template_id, template_loadout_id, perk_item_sdb_id, level)
    SELECT p_template_id, lm.template_loadout_id, k.perk_item_sdb_id, k.level
    FROM webapi."CharacterLoadoutPerks" k
    JOIN (SELECT loadout_id, row_number() OVER (ORDER BY loadout_id) AS template_loadout_id
          FROM webapi."CharacterLoadouts" WHERE character_guid = char_guid) lm USING (loadout_id);

    INSERT INTO webapi."NewCharacterTemplateBattleframes" (template_id, battleframe_sdb_id, level, elite_level, elite_points, hidden, visuals, bandwidth)
    SELECT p_template_id, battleframe_sdb_id, level, elite_level, elite_points, hidden, visuals, bandwidth
    FROM webapi."Battleframes"
    WHERE character_guid = char_guid;
END
$$;


ALTER FUNCTION webapi.snapshot_inventory_as_template(char_guid bigint, p_template_id integer) OWNER TO tmwadmin;

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
    rb_balance bigint NOT NULL,
    language text,
    login_ticket bytea,
    login_ticket_hash bytea,
    login_ticket_created_at timestamp with time zone
);


ALTER TABLE webapi."Accounts" OWNER TO tmwadmin;

--
-- Name: COLUMN "Accounts".login_ticket; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."Accounts".login_ticket IS 'The 32 random bytes behind this account''s login ticket, after the "R5 " header is stripped and before base64. The oracle/ticket route hands the client base64("R5 " + these bytes); the client gives the game server the decoded payload and the game server passes those bytes back for validation.';


--
-- Name: COLUMN "Accounts".login_ticket_hash; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."Accounts".login_ticket_hash IS 'SHA-256 of login_ticket. Validation hashes the presented ticket and looks this up, so the comparison never touches the raw column. Unique: two accounts must never accept the same ticket.';


--
-- Name: COLUMN "Accounts".login_ticket_created_at; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."Accounts".login_ticket_created_at IS 'When the current ticket was minted. Cleared together with the ticket when the password or signing secret changes.';


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


ALTER SEQUENCE webapi."ArmyApplications_id_seq" OWNER TO tmwadmin;

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
-- Name: BattleframeAvailableUpgrades; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."BattleframeAvailableUpgrades" (
    battleframe_guid bigint NOT NULL,
    upgrade_index integer NOT NULL,
    upgrade_id integer NOT NULL,
    category integer NOT NULL,
    stat_value real NOT NULL,
    count integer,
    item_sdb_id integer,
    localization_id integer NOT NULL
);


ALTER TABLE webapi."BattleframeAvailableUpgrades" OWNER TO tmwadmin;

--
-- Name: BattleframePreviousUpgrades; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."BattleframePreviousUpgrades" (
    battleframe_guid bigint NOT NULL,
    upgrade_id integer NOT NULL,
    count integer DEFAULT 0 NOT NULL
);


ALTER TABLE webapi."BattleframePreviousUpgrades" OWNER TO tmwadmin;

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
    current_xp bigint DEFAULT 0 NOT NULL,
    lifetime_xp bigint DEFAULT 0 NOT NULL,
    elite_level integer DEFAULT 0 NOT NULL,
    current_elite_xp bigint DEFAULT 0 NOT NULL,
    lifetime_elite_xp bigint DEFAULT 0 NOT NULL,
    elite_points integer DEFAULT 0 NOT NULL,
    bandwidth integer DEFAULT 0 NOT NULL
);


ALTER TABLE webapi."Battleframes" OWNER TO tmwadmin;

--
-- Name: COLUMN "Battleframes".bandwidth; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."Battleframes".bandwidth IS 'Perk point budget for this battleframe, shared by all of its loadouts. PIN computes it on level up and writes it; RIN stores it and never validates perk totals or the 10 perk cap against it.';


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
-- Name: CharacterItemModules; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."CharacterItemModules" (
    item_guid bigint NOT NULL,
    slot_index smallint NOT NULL,
    module_sdb_id integer NOT NULL
);


ALTER TABLE webapi."CharacterItemModules" OWNER TO tmwadmin;

--
-- Name: CharacterItems; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."CharacterItems" (
    item_guid bigint DEFAULT webapi.create_entity_guid(253) NOT NULL,
    character_guid bigint NOT NULL,
    sdb_id integer NOT NULL,
    sub_inventory smallint DEFAULT 1 NOT NULL,
    dynamic_flags smallint DEFAULT 0 NOT NULL,
    durability integer DEFAULT 1000 NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    loadout_id bigint
);


ALTER TABLE webapi."CharacterItems" OWNER TO tmwadmin;

--
-- Name: COLUMN "CharacterItems".loadout_id; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."CharacterItems".loadout_id IS 'Set on the one item that is a loadout''s chassis: the item the client API names as a garage slot''s item_guid. NULL on ordinary items. Unique where set, so a loadout has exactly one chassis, and ON DELETE CASCADE so the item goes with the loadout.';


--
-- Name: CharacterLoadoutPerks; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."CharacterLoadoutPerks" (
    id bigint NOT NULL,
    loadout_id integer NOT NULL,
    perk_item_sdb_id integer NOT NULL,
    level smallint DEFAULT 0 NOT NULL
);


ALTER TABLE webapi."CharacterLoadoutPerks" OWNER TO tmwadmin;

--
-- Name: TABLE "CharacterLoadoutPerks"; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON TABLE webapi."CharacterLoadoutPerks" IS 'Perk modules the player has slotted into a loadout. Identified by sdb id (dbitems::PerkModule.id), not by guid. PIN enforces the 10 perk cap and the frame bandwidth.';


--
-- Name: CharacterLoadoutPerks_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE webapi."CharacterLoadoutPerks" ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME webapi."CharacterLoadoutPerks_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: CharacterLoadoutSlots; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."CharacterLoadoutSlots" (
    id bigint NOT NULL,
    loadout_id integer NOT NULL,
    config_id smallint DEFAULT 0 NOT NULL,
    slot_index smallint NOT NULL,
    item_guid bigint NOT NULL
);


ALTER TABLE webapi."CharacterLoadoutSlots" OWNER TO tmwadmin;

--
-- Name: CharacterLoadoutSlots_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE webapi."CharacterLoadoutSlots" ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME webapi."CharacterLoadoutSlots_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: LoadoutId_Seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

CREATE SEQUENCE webapi."LoadoutId_Seq"
    START WITH 20001
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE webapi."LoadoutId_Seq" OWNER TO tmwadmin;

--
-- Name: CharacterLoadouts; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."CharacterLoadouts" (
    loadout_id integer DEFAULT nextval('webapi."LoadoutId_Seq"'::regclass) NOT NULL,
    character_guid bigint NOT NULL,
    chassis_id integer NOT NULL,
    loadout_name text DEFAULT 'Loadout'::text NOT NULL,
    loadout_type text DEFAULT 'battleframe'::text NOT NULL
);


ALTER TABLE webapi."CharacterLoadouts" OWNER TO tmwadmin;

--
-- Name: CharacterResources; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."CharacterResources" (
    id bigint NOT NULL,
    character_guid bigint NOT NULL,
    sdb_id integer NOT NULL,
    quantity integer DEFAULT 0 NOT NULL,
    sub_inventory smallint DEFAULT 1 NOT NULL
);


ALTER TABLE webapi."CharacterResources" OWNER TO tmwadmin;

--
-- Name: CharacterResources_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE webapi."CharacterResources" ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME webapi."CharacterResources_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: CharacterUnlocks; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."CharacterUnlocks" (
    character_guid bigint NOT NULL,
    unlock_type text NOT NULL,
    unlock_id integer NOT NULL,
    frame_id integer DEFAULT 0 NOT NULL,
    unlocked_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE webapi."CharacterUnlocks" OWNER TO tmwadmin;

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


ALTER SEQUENCE webapi."FFUUID_Counter_Seq" OWNER TO tmwadmin;

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


ALTER SEQUENCE webapi."Leaderboards_id_seq" OWNER TO tmwadmin;

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
-- Name: NewCharacterTemplateBattleframes; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."NewCharacterTemplateBattleframes" (
    template_id integer NOT NULL,
    battleframe_sdb_id integer NOT NULL,
    level integer DEFAULT 1 NOT NULL,
    elite_level integer DEFAULT 0 NOT NULL,
    elite_points integer DEFAULT 0 NOT NULL,
    hidden boolean DEFAULT false NOT NULL,
    visuals bytea DEFAULT '\x1089081884c2fcff0f188080cce609188080988c03188080a4d20418aef7c9a50918c1e280e40c18c1e280e40c'::bytea NOT NULL,
    bandwidth integer DEFAULT 0 NOT NULL,
    CONSTRAINT template_battleframes_level_check CHECK ((level >= 1))
);


ALTER TABLE webapi."NewCharacterTemplateBattleframes" OWNER TO tmwadmin;

--
-- Name: COLUMN "NewCharacterTemplateBattleframes".visuals; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."NewCharacterTemplateBattleframes".visuals IS 'Serialised PlayerBattleframeVisuals. The default is PlayerBattleframeVisuals.CreateDefault().';


--
-- Name: NewCharacterTemplateItemModules; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."NewCharacterTemplateItemModules" (
    template_item_id integer NOT NULL,
    slot_index smallint NOT NULL,
    module_sdb_id integer NOT NULL,
    template_id integer DEFAULT 1 NOT NULL
);


ALTER TABLE webapi."NewCharacterTemplateItemModules" OWNER TO tmwadmin;

--
-- Name: NewCharacterTemplateItems; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."NewCharacterTemplateItems" (
    template_item_id integer NOT NULL,
    sdb_id integer NOT NULL,
    sub_inventory smallint DEFAULT 1 NOT NULL,
    dynamic_flags smallint DEFAULT 0 NOT NULL,
    durability integer DEFAULT 1000 NOT NULL,
    template_id integer DEFAULT 1 NOT NULL
);


ALTER TABLE webapi."NewCharacterTemplateItems" OWNER TO tmwadmin;

--
-- Name: NewCharacterTemplateLoadoutPerks; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."NewCharacterTemplateLoadoutPerks" (
    template_id integer NOT NULL,
    template_loadout_id integer NOT NULL,
    perk_item_sdb_id integer NOT NULL,
    level smallint DEFAULT 0 NOT NULL
);


ALTER TABLE webapi."NewCharacterTemplateLoadoutPerks" OWNER TO tmwadmin;

--
-- Name: NewCharacterTemplateLoadoutSlots; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."NewCharacterTemplateLoadoutSlots" (
    template_loadout_id integer NOT NULL,
    config_id smallint DEFAULT 0 NOT NULL,
    slot_index smallint NOT NULL,
    template_item_id integer NOT NULL,
    template_id integer DEFAULT 1 NOT NULL
);


ALTER TABLE webapi."NewCharacterTemplateLoadoutSlots" OWNER TO tmwadmin;

--
-- Name: NewCharacterTemplateLoadouts; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."NewCharacterTemplateLoadouts" (
    template_loadout_id integer NOT NULL,
    chassis_id integer NOT NULL,
    loadout_name text DEFAULT 'Loadout'::text NOT NULL,
    loadout_type text DEFAULT 'battleframe'::text NOT NULL,
    template_id integer DEFAULT 1 NOT NULL
);


ALTER TABLE webapi."NewCharacterTemplateLoadouts" OWNER TO tmwadmin;

--
-- Name: NewCharacterTemplateResources; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."NewCharacterTemplateResources" (
    sdb_id integer NOT NULL,
    quantity integer DEFAULT 0 NOT NULL,
    sub_inventory smallint DEFAULT 1 NOT NULL,
    template_id integer DEFAULT 1 NOT NULL
);


ALTER TABLE webapi."NewCharacterTemplateResources" OWNER TO tmwadmin;

--
-- Name: NewCharacterTemplates; Type: TABLE; Schema: webapi; Owner: tmwadmin
--

CREATE TABLE webapi."NewCharacterTemplates" (
    template_id integer NOT NULL,
    name text NOT NULL,
    is_default boolean DEFAULT false NOT NULL,
    description text DEFAULT ''::text NOT NULL
);


ALTER TABLE webapi."NewCharacterTemplates" OWNER TO tmwadmin;

--
-- Name: COLUMN "NewCharacterTemplates".is_default; Type: COMMENT; Schema: webapi; Owner: tmwadmin
--

COMMENT ON COLUMN webapi."NewCharacterTemplates".is_default IS 'The template used when template_id 0 is passed, i.e. what CreateNewCharacter and SeedCharacterFromTemplate seed from. Exactly one row may have it.';


--
-- Name: NewCharacterTemplates_template_id_seq; Type: SEQUENCE; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE webapi."NewCharacterTemplates" ALTER COLUMN template_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME webapi."NewCharacterTemplates_template_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


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


ALTER SEQUENCE webapi."ZoneCertificates_id_seq" OWNER TO tmwadmin;

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


ALTER SEQUENCE webapi."ZoneDifficulty_id_seq" OWNER TO tmwadmin;

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


ALTER SEQUENCE webapi."ZoneSettings_id_seq" OWNER TO tmwadmin;

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
-- Name: CharacterItemModules CharacterItemModules_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterItemModules"
    ADD CONSTRAINT "CharacterItemModules_pkey" PRIMARY KEY (item_guid, slot_index);


--
-- Name: CharacterItems CharacterItems_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterItems"
    ADD CONSTRAINT "CharacterItems_pkey" PRIMARY KEY (item_guid);


--
-- Name: CharacterLoadoutPerks CharacterLoadoutPerks_loadout_perk_key; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterLoadoutPerks"
    ADD CONSTRAINT "CharacterLoadoutPerks_loadout_perk_key" UNIQUE (loadout_id, perk_item_sdb_id);


--
-- Name: CharacterLoadoutPerks CharacterLoadoutPerks_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterLoadoutPerks"
    ADD CONSTRAINT "CharacterLoadoutPerks_pkey" PRIMARY KEY (id);


--
-- Name: CharacterLoadoutSlots CharacterLoadoutSlots_loadout_id_config_id_slot_index_key; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterLoadoutSlots"
    ADD CONSTRAINT "CharacterLoadoutSlots_loadout_id_config_id_slot_index_key" UNIQUE (loadout_id, config_id, slot_index);


--
-- Name: CharacterLoadoutSlots CharacterLoadoutSlots_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterLoadoutSlots"
    ADD CONSTRAINT "CharacterLoadoutSlots_pkey" PRIMARY KEY (id);


--
-- Name: CharacterLoadouts CharacterLoadouts_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterLoadouts"
    ADD CONSTRAINT "CharacterLoadouts_pkey" PRIMARY KEY (loadout_id);


--
-- Name: CharacterResources CharacterResources_character_guid_sdb_id_key; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterResources"
    ADD CONSTRAINT "CharacterResources_character_guid_sdb_id_key" UNIQUE (character_guid, sdb_id);


--
-- Name: CharacterResources CharacterResources_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterResources"
    ADD CONSTRAINT "CharacterResources_pkey" PRIMARY KEY (id);


--
-- Name: CharacterUnlocks CharacterUnlocks_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterUnlocks"
    ADD CONSTRAINT "CharacterUnlocks_pkey" PRIMARY KEY (character_guid, unlock_type, unlock_id, frame_id);


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
-- Name: NewCharacterTemplateBattleframes NewCharacterTemplateBattleframes_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateBattleframes"
    ADD CONSTRAINT "NewCharacterTemplateBattleframes_pkey" PRIMARY KEY (template_id, battleframe_sdb_id);


--
-- Name: NewCharacterTemplateItemModules NewCharacterTemplateItemModules_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateItemModules"
    ADD CONSTRAINT "NewCharacterTemplateItemModules_pkey" PRIMARY KEY (template_id, template_item_id, slot_index);


--
-- Name: NewCharacterTemplateItems NewCharacterTemplateItems_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateItems"
    ADD CONSTRAINT "NewCharacterTemplateItems_pkey" PRIMARY KEY (template_id, template_item_id);


--
-- Name: NewCharacterTemplateLoadoutPerks NewCharacterTemplateLoadoutPerks_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutPerks"
    ADD CONSTRAINT "NewCharacterTemplateLoadoutPerks_pkey" PRIMARY KEY (template_id, template_loadout_id, perk_item_sdb_id);


--
-- Name: NewCharacterTemplateLoadoutSlots NewCharacterTemplateLoadoutSlots_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutSlots"
    ADD CONSTRAINT "NewCharacterTemplateLoadoutSlots_pkey" PRIMARY KEY (template_id, template_loadout_id, config_id, slot_index);


--
-- Name: NewCharacterTemplateLoadouts NewCharacterTemplateLoadouts_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadouts"
    ADD CONSTRAINT "NewCharacterTemplateLoadouts_pkey" PRIMARY KEY (template_id, template_loadout_id);


--
-- Name: NewCharacterTemplateResources NewCharacterTemplateResources_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateResources"
    ADD CONSTRAINT "NewCharacterTemplateResources_pkey" PRIMARY KEY (template_id, sdb_id);


--
-- Name: NewCharacterTemplates NewCharacterTemplates_name_key; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplates"
    ADD CONSTRAINT "NewCharacterTemplates_name_key" UNIQUE (name);


--
-- Name: NewCharacterTemplates NewCharacterTemplates_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplates"
    ADD CONSTRAINT "NewCharacterTemplates_pkey" PRIMARY KEY (template_id);


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
-- Name: BattleframeAvailableUpgrades battleframe_available_upgrades_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."BattleframeAvailableUpgrades"
    ADD CONSTRAINT battleframe_available_upgrades_pkey PRIMARY KEY (battleframe_guid, upgrade_index);


--
-- Name: BattleframePreviousUpgrades battleframe_previous_upgrades_pkey; Type: CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."BattleframePreviousUpgrades"
    ADD CONSTRAINT battleframe_previous_upgrades_pkey PRIMARY KEY (battleframe_guid, upgrade_id);


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
-- Name: accounts_login_ticket_hash_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX accounts_login_ticket_hash_uindex ON webapi."Accounts" USING btree (login_ticket_hash);


--
-- Name: accounts_uid_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX accounts_uid_uindex ON webapi."Accounts" USING btree (uid);


--
-- Name: character_items_loadout_id_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX character_items_loadout_id_uindex ON webapi."CharacterItems" USING btree (loadout_id) WHERE (loadout_id IS NOT NULL);


--
-- Name: characters_name_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX characters_name_uindex ON webapi."Characters" USING btree (name);


--
-- Name: idx_character_unlocks_character; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE INDEX idx_character_unlocks_character ON webapi."CharacterUnlocks" USING btree (character_guid);


--
-- Name: idx_charitems_char; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE INDEX idx_charitems_char ON webapi."CharacterItems" USING btree (character_guid);


--
-- Name: idx_charloadouts_char; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE INDEX idx_charloadouts_char ON webapi."CharacterLoadouts" USING btree (character_guid);


--
-- Name: login_events_id_uindex; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX login_events_id_uindex ON webapi."LoginEvents" USING btree (id);


--
-- Name: new_character_templates_single_default; Type: INDEX; Schema: webapi; Owner: tmwadmin
--

CREATE UNIQUE INDEX new_character_templates_single_default ON webapi."NewCharacterTemplates" USING btree (is_default) WHERE is_default;


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
-- Name: Accounts accounts_password_change_invalidates_ticket; Type: TRIGGER; Schema: webapi; Owner: tmwadmin
--

CREATE TRIGGER accounts_password_change_invalidates_ticket BEFORE UPDATE OF password_hash, secret ON webapi."Accounts" FOR EACH ROW WHEN (((old.password_hash IS DISTINCT FROM new.password_hash) OR (old.secret IS DISTINCT FROM new.secret))) EXECUTE FUNCTION webapi."InvalidateLoginTicketOnPasswordChange"();


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
-- Name: CharacterUnlocks character_unlock_events_trigger; Type: TRIGGER; Schema: webapi; Owner: tmwadmin
--

CREATE TRIGGER character_unlock_events_trigger AFTER INSERT OR DELETE ON webapi."CharacterUnlocks" FOR EACH ROW EXECUTE FUNCTION webapi.notify_on_unlock_events();


--
-- Name: CharacterLoadouts loadout_chassis_item_trigger; Type: TRIGGER; Schema: webapi; Owner: tmwadmin
--

CREATE TRIGGER loadout_chassis_item_trigger AFTER INSERT ON webapi."CharacterLoadouts" FOR EACH ROW WHEN ((new.loadout_type = 'battleframe'::text)) EXECUTE FUNCTION webapi.create_chassis_item_for_loadout();


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
-- Name: CharacterLoadoutPerks CharacterLoadoutPerks_loadout_id_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterLoadoutPerks"
    ADD CONSTRAINT "CharacterLoadoutPerks_loadout_id_fkey" FOREIGN KEY (loadout_id) REFERENCES webapi."CharacterLoadouts"(loadout_id) ON DELETE CASCADE;


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
-- Name: BattleframeAvailableUpgrades battleframe_available_upgrades_battleframe_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."BattleframeAvailableUpgrades"
    ADD CONSTRAINT battleframe_available_upgrades_battleframe_guid_fkey FOREIGN KEY (battleframe_guid) REFERENCES webapi."Battleframes"(id) ON DELETE CASCADE;


--
-- Name: BattleframePreviousUpgrades battleframe_previous_upgrades_battleframe_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."BattleframePreviousUpgrades"
    ADD CONSTRAINT battleframe_previous_upgrades_battleframe_guid_fkey FOREIGN KEY (battleframe_guid) REFERENCES webapi."Battleframes"(id) ON DELETE CASCADE;


--
-- Name: ArmyMembers character_guid; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."ArmyMembers"
    ADD CONSTRAINT character_guid FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid);


--
-- Name: CharacterItemModules character_item_modules_item_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterItemModules"
    ADD CONSTRAINT character_item_modules_item_guid_fkey FOREIGN KEY (item_guid) REFERENCES webapi."CharacterItems"(item_guid) ON DELETE CASCADE;


--
-- Name: CharacterItems character_items_character_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterItems"
    ADD CONSTRAINT character_items_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE;


--
-- Name: CharacterItems character_items_loadout_id_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterItems"
    ADD CONSTRAINT character_items_loadout_id_fkey FOREIGN KEY (loadout_id) REFERENCES webapi."CharacterLoadouts"(loadout_id) ON DELETE CASCADE;


--
-- Name: CharacterLoadoutSlots character_loadout_slots_item_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterLoadoutSlots"
    ADD CONSTRAINT character_loadout_slots_item_guid_fkey FOREIGN KEY (item_guid) REFERENCES webapi."CharacterItems"(item_guid) ON DELETE CASCADE;


--
-- Name: CharacterLoadoutSlots character_loadout_slots_loadout_id_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterLoadoutSlots"
    ADD CONSTRAINT character_loadout_slots_loadout_id_fkey FOREIGN KEY (loadout_id) REFERENCES webapi."CharacterLoadouts"(loadout_id) ON DELETE CASCADE;


--
-- Name: CharacterLoadouts character_loadouts_character_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterLoadouts"
    ADD CONSTRAINT character_loadouts_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE;


--
-- Name: CharacterResources character_resources_character_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterResources"
    ADD CONSTRAINT character_resources_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE;


--
-- Name: CharacterUnlocks character_unlocks_character_guid_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."CharacterUnlocks"
    ADD CONSTRAINT character_unlocks_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE;


--
-- Name: NewCharacterTemplateBattleframes template_battleframes_template_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateBattleframes"
    ADD CONSTRAINT template_battleframes_template_fkey FOREIGN KEY (template_id) REFERENCES webapi."NewCharacterTemplates"(template_id) ON DELETE CASCADE;


--
-- Name: NewCharacterTemplateItemModules template_item_modules_item_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateItemModules"
    ADD CONSTRAINT template_item_modules_item_fkey FOREIGN KEY (template_id, template_item_id) REFERENCES webapi."NewCharacterTemplateItems"(template_id, template_item_id) ON DELETE CASCADE;


--
-- Name: NewCharacterTemplateItems template_items_template_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateItems"
    ADD CONSTRAINT template_items_template_fkey FOREIGN KEY (template_id) REFERENCES webapi."NewCharacterTemplates"(template_id) ON DELETE CASCADE;


--
-- Name: NewCharacterTemplateLoadoutPerks template_loadout_perks_loadout_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutPerks"
    ADD CONSTRAINT template_loadout_perks_loadout_fkey FOREIGN KEY (template_id, template_loadout_id) REFERENCES webapi."NewCharacterTemplateLoadouts"(template_id, template_loadout_id) ON DELETE CASCADE;


--
-- Name: NewCharacterTemplateLoadoutSlots template_loadout_slots_item_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutSlots"
    ADD CONSTRAINT template_loadout_slots_item_fkey FOREIGN KEY (template_id, template_item_id) REFERENCES webapi."NewCharacterTemplateItems"(template_id, template_item_id) ON DELETE CASCADE;


--
-- Name: NewCharacterTemplateLoadoutSlots template_loadout_slots_loadout_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutSlots"
    ADD CONSTRAINT template_loadout_slots_loadout_fkey FOREIGN KEY (template_id, template_loadout_id) REFERENCES webapi."NewCharacterTemplateLoadouts"(template_id, template_loadout_id) ON DELETE CASCADE;


--
-- Name: NewCharacterTemplateLoadouts template_loadouts_template_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadouts"
    ADD CONSTRAINT template_loadouts_template_fkey FOREIGN KEY (template_id) REFERENCES webapi."NewCharacterTemplates"(template_id) ON DELETE CASCADE;


--
-- Name: NewCharacterTemplateResources template_resources_template_fkey; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."NewCharacterTemplateResources"
    ADD CONSTRAINT template_resources_template_fkey FOREIGN KEY (template_id) REFERENCES webapi."NewCharacterTemplates"(template_id) ON DELETE CASCADE;


--
-- Name: VipData vipdata_accounts_account_id_fk; Type: FK CONSTRAINT; Schema: webapi; Owner: tmwadmin
--

ALTER TABLE ONLY webapi."VipData"
    ADD CONSTRAINT vipdata_accounts_account_id_fk FOREIGN KEY (account_id) REFERENCES webapi."Accounts"(account_id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict BTbasBS3VXkGFnU2xx53bKM25c9zv0oK9FdpuVbGmQap1NLDI6d5nTkc8gviWmv

