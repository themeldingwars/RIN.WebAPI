--
-- Inventory, character templates, battleframe progression, login tickets and loadout visuals.
--
-- Safe to run against an existing database, and safe to run twice. Load
-- sql/Data/NewCharacterTemplate.sql afterwards to fill the templates.

BEGIN;

-- ---------------------------------------------------------------------------
-- Battleframe progression: xp splits into current and lifetime
-- ---------------------------------------------------------------------------

ALTER TABLE webapi."Battleframes"
    ADD COLUMN IF NOT EXISTS current_xp       bigint  DEFAULT 0 NOT NULL,
    ADD COLUMN IF NOT EXISTS lifetime_xp      bigint  DEFAULT 0 NOT NULL,
    ADD COLUMN IF NOT EXISTS elite_level      integer DEFAULT 0 NOT NULL,
    ADD COLUMN IF NOT EXISTS current_elite_xp bigint  DEFAULT 0 NOT NULL,
    ADD COLUMN IF NOT EXISTS lifetime_elite_xp bigint DEFAULT 0 NOT NULL,
    ADD COLUMN IF NOT EXISTS elite_points     integer DEFAULT 0 NOT NULL,
    ADD COLUMN IF NOT EXISTS bandwidth        integer DEFAULT 0 NOT NULL;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_schema = 'webapi' AND table_name = 'Battleframes'
                 AND column_name = 'xp') THEN
        UPDATE webapi."Battleframes" SET current_xp = xp, lifetime_xp = xp WHERE xp > 0;
    END IF;
END
$$;

ALTER TABLE webapi."Battleframes" DROP COLUMN IF EXISTS xp;

-- ---------------------------------------------------------------------------
-- Login tickets
-- ---------------------------------------------------------------------------

ALTER TABLE webapi."Accounts"
    ADD COLUMN IF NOT EXISTS login_ticket            bytea,
    ADD COLUMN IF NOT EXISTS login_ticket_hash       bytea,
    ADD COLUMN IF NOT EXISTS login_ticket_created_at timestamp with time zone;


-- ---------------------------------------------------------------------------
-- Sequences
-- ---------------------------------------------------------------------------

CREATE SEQUENCE IF NOT EXISTS webapi."LoadoutId_Seq"
    START WITH 20001
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER SEQUENCE webapi."LoadoutId_Seq" OWNER TO tmwadmin;

-- ---------------------------------------------------------------------------
-- Tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS webapi."BattleframeAvailableUpgrades" (
    battleframe_guid bigint NOT NULL,
    upgrade_index integer NOT NULL,
    upgrade_id integer NOT NULL,
    category integer NOT NULL,
    stat_value real NOT NULL,
    count integer,
    item_sdb_id integer,
    localization_id integer NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."BattleframePreviousUpgrades" (
    battleframe_guid bigint NOT NULL,
    upgrade_id integer NOT NULL,
    count integer DEFAULT 0 NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."CharacterItemModules" (
    item_guid bigint NOT NULL,
    slot_index smallint NOT NULL,
    module_sdb_id integer NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."CharacterItems" (
    item_guid bigint DEFAULT webapi.create_entity_guid(253) NOT NULL,
    character_guid bigint NOT NULL,
    sdb_id integer NOT NULL,
    sub_inventory smallint DEFAULT 1 NOT NULL,
    dynamic_flags smallint DEFAULT 0 NOT NULL,
    durability integer DEFAULT 1000 NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    loadout_id bigint
);

CREATE TABLE IF NOT EXISTS webapi."CharacterLoadoutPerks" (
    id bigint NOT NULL,
    loadout_id integer NOT NULL,
    perk_item_sdb_id integer NOT NULL,
    level smallint DEFAULT 0 NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."CharacterLoadoutSlots" (
    id bigint NOT NULL,
    loadout_id integer NOT NULL,
    config_id smallint DEFAULT 0 NOT NULL,
    slot_index smallint NOT NULL,
    item_guid bigint NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."CharacterLoadoutVisuals" (
    id bigint NOT NULL,
    loadout_id integer NOT NULL,
    config_id smallint DEFAULT 0 NOT NULL,
    visuals jsonb DEFAULT '[]'::jsonb NOT NULL,
    CONSTRAINT "CharacterLoadoutVisuals_visuals_is_array" CHECK ((jsonb_typeof(visuals) = 'array'::text))
);

CREATE TABLE IF NOT EXISTS webapi."CharacterLoadouts" (
    loadout_id integer DEFAULT nextval('webapi."LoadoutId_Seq"'::regclass) NOT NULL,
    character_guid bigint NOT NULL,
    chassis_id integer NOT NULL,
    loadout_name text DEFAULT 'Loadout'::text NOT NULL,
    loadout_type text DEFAULT 'battleframe'::text NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."CharacterResources" (
    id bigint NOT NULL,
    character_guid bigint NOT NULL,
    sdb_id integer NOT NULL,
    quantity integer DEFAULT 0 NOT NULL,
    sub_inventory smallint DEFAULT 1 NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."CharacterUnlocks" (
    character_guid bigint NOT NULL,
    unlock_type text NOT NULL,
    unlock_id integer NOT NULL,
    frame_id integer DEFAULT 0 NOT NULL,
    unlocked_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."NewCharacterTemplateBattleframes" (
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

CREATE TABLE IF NOT EXISTS webapi."NewCharacterTemplateItemModules" (
    template_item_id integer NOT NULL,
    slot_index smallint NOT NULL,
    module_sdb_id integer NOT NULL,
    template_id integer DEFAULT 1 NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."NewCharacterTemplateItems" (
    template_item_id integer NOT NULL,
    sdb_id integer NOT NULL,
    sub_inventory smallint DEFAULT 1 NOT NULL,
    dynamic_flags smallint DEFAULT 0 NOT NULL,
    durability integer DEFAULT 1000 NOT NULL,
    template_id integer DEFAULT 1 NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."NewCharacterTemplateLoadoutPerks" (
    template_id integer NOT NULL,
    template_loadout_id integer NOT NULL,
    perk_item_sdb_id integer NOT NULL,
    level smallint DEFAULT 0 NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."NewCharacterTemplateLoadoutSlots" (
    template_loadout_id integer NOT NULL,
    config_id smallint DEFAULT 0 NOT NULL,
    slot_index smallint NOT NULL,
    template_item_id integer NOT NULL,
    template_id integer DEFAULT 1 NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."NewCharacterTemplateLoadoutVisuals" (
    template_id integer NOT NULL,
    template_loadout_id integer NOT NULL,
    config_id smallint DEFAULT 0 NOT NULL,
    visuals jsonb DEFAULT '[]'::jsonb NOT NULL,
    CONSTRAINT "NewCharacterTemplateLoadoutVisuals_visuals_is_array" CHECK ((jsonb_typeof(visuals) = 'array'::text))
);

CREATE TABLE IF NOT EXISTS webapi."NewCharacterTemplateLoadouts" (
    template_loadout_id integer NOT NULL,
    chassis_id integer NOT NULL,
    loadout_name text DEFAULT 'Loadout'::text NOT NULL,
    loadout_type text DEFAULT 'battleframe'::text NOT NULL,
    template_id integer DEFAULT 1 NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."NewCharacterTemplateResources" (
    sdb_id integer NOT NULL,
    quantity integer DEFAULT 0 NOT NULL,
    sub_inventory smallint DEFAULT 1 NOT NULL,
    template_id integer DEFAULT 1 NOT NULL
);

CREATE TABLE IF NOT EXISTS webapi."NewCharacterTemplates" (
    template_id integer NOT NULL,
    name text NOT NULL,
    is_default boolean DEFAULT false NOT NULL,
    description text DEFAULT ''::text NOT NULL
);

ALTER TABLE webapi."BattleframeAvailableUpgrades" OWNER TO tmwadmin;

ALTER TABLE webapi."BattleframePreviousUpgrades" OWNER TO tmwadmin;

ALTER TABLE webapi."CharacterItemModules" OWNER TO tmwadmin;

ALTER TABLE webapi."CharacterItems" OWNER TO tmwadmin;

ALTER TABLE webapi."CharacterLoadoutPerks" OWNER TO tmwadmin;

ALTER TABLE webapi."CharacterLoadoutSlots" OWNER TO tmwadmin;

ALTER TABLE webapi."CharacterLoadoutVisuals" OWNER TO tmwadmin;

ALTER TABLE webapi."CharacterLoadouts" OWNER TO tmwadmin;

ALTER TABLE webapi."CharacterResources" OWNER TO tmwadmin;

ALTER TABLE webapi."CharacterUnlocks" OWNER TO tmwadmin;

ALTER TABLE webapi."NewCharacterTemplateBattleframes" OWNER TO tmwadmin;

ALTER TABLE webapi."NewCharacterTemplateItemModules" OWNER TO tmwadmin;

ALTER TABLE webapi."NewCharacterTemplateItems" OWNER TO tmwadmin;

ALTER TABLE webapi."NewCharacterTemplateLoadoutPerks" OWNER TO tmwadmin;

ALTER TABLE webapi."NewCharacterTemplateLoadoutSlots" OWNER TO tmwadmin;

ALTER TABLE webapi."NewCharacterTemplateLoadoutVisuals" OWNER TO tmwadmin;

ALTER TABLE webapi."NewCharacterTemplateLoadouts" OWNER TO tmwadmin;

ALTER TABLE webapi."NewCharacterTemplateResources" OWNER TO tmwadmin;

ALTER TABLE webapi."NewCharacterTemplates" OWNER TO tmwadmin;

DO $$
BEGIN
    IF (SELECT is_identity FROM information_schema.columns
        WHERE table_schema = 'webapi' AND table_name = 'CharacterLoadoutPerks' AND column_name = 'id') = 'NO' THEN
        ALTER TABLE webapi."CharacterLoadoutPerks" ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME webapi."CharacterLoadoutPerks_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);
    END IF;
END
$$;

DO $$
BEGIN
    IF (SELECT is_identity FROM information_schema.columns
        WHERE table_schema = 'webapi' AND table_name = 'CharacterLoadoutSlots' AND column_name = 'id') = 'NO' THEN
        ALTER TABLE webapi."CharacterLoadoutSlots" ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME webapi."CharacterLoadoutSlots_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);
    END IF;
END
$$;

DO $$
BEGIN
    IF (SELECT is_identity FROM information_schema.columns
        WHERE table_schema = 'webapi' AND table_name = 'CharacterLoadoutVisuals' AND column_name = 'id') = 'NO' THEN
        ALTER TABLE webapi."CharacterLoadoutVisuals" ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME webapi."CharacterLoadoutVisuals_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);
    END IF;
END
$$;

DO $$
BEGIN
    IF (SELECT is_identity FROM information_schema.columns
        WHERE table_schema = 'webapi' AND table_name = 'CharacterResources' AND column_name = 'id') = 'NO' THEN
        ALTER TABLE webapi."CharacterResources" ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME webapi."CharacterResources_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);
    END IF;
END
$$;

DO $$
BEGIN
    IF (SELECT is_identity FROM information_schema.columns
        WHERE table_schema = 'webapi' AND table_name = 'NewCharacterTemplates' AND column_name = 'template_id') = 'NO' THEN
        ALTER TABLE webapi."NewCharacterTemplates" ALTER COLUMN template_id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME webapi."NewCharacterTemplates_template_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);
    END IF;
END
$$;

-- ---------------------------------------------------------------------------
-- Keys and foreign keys
-- ---------------------------------------------------------------------------

ALTER TABLE webapi."CharacterItemModules" DROP CONSTRAINT IF EXISTS "CharacterItemModules_pkey" CASCADE;
ALTER TABLE webapi."CharacterItems" DROP CONSTRAINT IF EXISTS "CharacterItems_pkey" CASCADE;
ALTER TABLE webapi."CharacterLoadoutPerks" DROP CONSTRAINT IF EXISTS "CharacterLoadoutPerks_loadout_perk_key" CASCADE;
ALTER TABLE webapi."CharacterLoadoutPerks" DROP CONSTRAINT IF EXISTS "CharacterLoadoutPerks_pkey" CASCADE;
ALTER TABLE webapi."CharacterLoadoutSlots" DROP CONSTRAINT IF EXISTS "CharacterLoadoutSlots_loadout_id_config_id_slot_index_key" CASCADE;
ALTER TABLE webapi."CharacterLoadoutSlots" DROP CONSTRAINT IF EXISTS "CharacterLoadoutSlots_pkey" CASCADE;
ALTER TABLE webapi."CharacterLoadoutVisuals" DROP CONSTRAINT IF EXISTS "CharacterLoadoutVisuals_loadout_config_key" CASCADE;
ALTER TABLE webapi."CharacterLoadoutVisuals" DROP CONSTRAINT IF EXISTS "CharacterLoadoutVisuals_pkey" CASCADE;
ALTER TABLE webapi."CharacterLoadouts" DROP CONSTRAINT IF EXISTS "CharacterLoadouts_pkey" CASCADE;
ALTER TABLE webapi."CharacterResources" DROP CONSTRAINT IF EXISTS "CharacterResources_character_guid_sdb_id_key" CASCADE;
ALTER TABLE webapi."CharacterResources" DROP CONSTRAINT IF EXISTS "CharacterResources_pkey" CASCADE;
ALTER TABLE webapi."CharacterUnlocks" DROP CONSTRAINT IF EXISTS "CharacterUnlocks_pkey" CASCADE;
ALTER TABLE webapi."NewCharacterTemplateBattleframes" DROP CONSTRAINT IF EXISTS "NewCharacterTemplateBattleframes_pkey" CASCADE;
ALTER TABLE webapi."NewCharacterTemplateItemModules" DROP CONSTRAINT IF EXISTS "NewCharacterTemplateItemModules_pkey" CASCADE;
ALTER TABLE webapi."NewCharacterTemplateItems" DROP CONSTRAINT IF EXISTS "NewCharacterTemplateItems_pkey" CASCADE;
ALTER TABLE webapi."NewCharacterTemplateLoadoutPerks" DROP CONSTRAINT IF EXISTS "NewCharacterTemplateLoadoutPerks_pkey" CASCADE;
ALTER TABLE webapi."NewCharacterTemplateLoadoutSlots" DROP CONSTRAINT IF EXISTS "NewCharacterTemplateLoadoutSlots_pkey" CASCADE;
ALTER TABLE webapi."NewCharacterTemplateLoadoutVisuals" DROP CONSTRAINT IF EXISTS "NewCharacterTemplateLoadoutVisuals_pkey" CASCADE;
ALTER TABLE webapi."NewCharacterTemplateLoadouts" DROP CONSTRAINT IF EXISTS "NewCharacterTemplateLoadouts_pkey" CASCADE;
ALTER TABLE webapi."NewCharacterTemplateResources" DROP CONSTRAINT IF EXISTS "NewCharacterTemplateResources_pkey" CASCADE;
ALTER TABLE webapi."NewCharacterTemplates" DROP CONSTRAINT IF EXISTS "NewCharacterTemplates_name_key" CASCADE;
ALTER TABLE webapi."NewCharacterTemplates" DROP CONSTRAINT IF EXISTS "NewCharacterTemplates_pkey" CASCADE;
ALTER TABLE webapi."BattleframeAvailableUpgrades" DROP CONSTRAINT IF EXISTS battleframe_available_upgrades_pkey CASCADE;
ALTER TABLE webapi."BattleframePreviousUpgrades" DROP CONSTRAINT IF EXISTS battleframe_previous_upgrades_pkey CASCADE;
ALTER TABLE webapi."CharacterLoadoutPerks" DROP CONSTRAINT IF EXISTS "CharacterLoadoutPerks_loadout_id_fkey" CASCADE;
ALTER TABLE webapi."CharacterLoadoutVisuals" DROP CONSTRAINT IF EXISTS "CharacterLoadoutVisuals_loadout_id_fkey" CASCADE;
ALTER TABLE webapi."BattleframeAvailableUpgrades" DROP CONSTRAINT IF EXISTS battleframe_available_upgrades_battleframe_guid_fkey CASCADE;
ALTER TABLE webapi."BattleframePreviousUpgrades" DROP CONSTRAINT IF EXISTS battleframe_previous_upgrades_battleframe_guid_fkey CASCADE;
ALTER TABLE webapi."CharacterItemModules" DROP CONSTRAINT IF EXISTS character_item_modules_item_guid_fkey CASCADE;
ALTER TABLE webapi."CharacterItems" DROP CONSTRAINT IF EXISTS character_items_character_guid_fkey CASCADE;
ALTER TABLE webapi."CharacterItems" DROP CONSTRAINT IF EXISTS character_items_loadout_id_fkey CASCADE;
ALTER TABLE webapi."CharacterLoadoutSlots" DROP CONSTRAINT IF EXISTS character_loadout_slots_item_guid_fkey CASCADE;
ALTER TABLE webapi."CharacterLoadoutSlots" DROP CONSTRAINT IF EXISTS character_loadout_slots_loadout_id_fkey CASCADE;
ALTER TABLE webapi."CharacterLoadouts" DROP CONSTRAINT IF EXISTS character_loadouts_character_guid_fkey CASCADE;
ALTER TABLE webapi."CharacterResources" DROP CONSTRAINT IF EXISTS character_resources_character_guid_fkey CASCADE;
ALTER TABLE webapi."CharacterUnlocks" DROP CONSTRAINT IF EXISTS character_unlocks_character_guid_fkey CASCADE;
ALTER TABLE webapi."NewCharacterTemplateBattleframes" DROP CONSTRAINT IF EXISTS template_battleframes_template_fkey CASCADE;
ALTER TABLE webapi."NewCharacterTemplateItemModules" DROP CONSTRAINT IF EXISTS template_item_modules_item_fkey CASCADE;
ALTER TABLE webapi."NewCharacterTemplateItems" DROP CONSTRAINT IF EXISTS template_items_template_fkey CASCADE;
ALTER TABLE webapi."NewCharacterTemplateLoadoutPerks" DROP CONSTRAINT IF EXISTS template_loadout_perks_loadout_fkey CASCADE;
ALTER TABLE webapi."NewCharacterTemplateLoadoutSlots" DROP CONSTRAINT IF EXISTS template_loadout_slots_item_fkey CASCADE;
ALTER TABLE webapi."NewCharacterTemplateLoadoutSlots" DROP CONSTRAINT IF EXISTS template_loadout_slots_loadout_fkey CASCADE;
ALTER TABLE webapi."NewCharacterTemplateLoadoutVisuals" DROP CONSTRAINT IF EXISTS template_loadout_visuals_loadout_fkey CASCADE;
ALTER TABLE webapi."NewCharacterTemplateLoadouts" DROP CONSTRAINT IF EXISTS template_loadouts_template_fkey CASCADE;
ALTER TABLE webapi."NewCharacterTemplateResources" DROP CONSTRAINT IF EXISTS template_resources_template_fkey CASCADE;

ALTER TABLE ONLY webapi."CharacterItemModules"
    ADD CONSTRAINT "CharacterItemModules_pkey" PRIMARY KEY (item_guid, slot_index);

ALTER TABLE ONLY webapi."CharacterItems"
    ADD CONSTRAINT "CharacterItems_pkey" PRIMARY KEY (item_guid);

ALTER TABLE ONLY webapi."CharacterLoadoutPerks"
    ADD CONSTRAINT "CharacterLoadoutPerks_loadout_perk_key" UNIQUE (loadout_id, perk_item_sdb_id);

ALTER TABLE ONLY webapi."CharacterLoadoutPerks"
    ADD CONSTRAINT "CharacterLoadoutPerks_pkey" PRIMARY KEY (id);

ALTER TABLE ONLY webapi."CharacterLoadoutSlots"
    ADD CONSTRAINT "CharacterLoadoutSlots_loadout_id_config_id_slot_index_key" UNIQUE (loadout_id, config_id, slot_index);

ALTER TABLE ONLY webapi."CharacterLoadoutSlots"
    ADD CONSTRAINT "CharacterLoadoutSlots_pkey" PRIMARY KEY (id);

ALTER TABLE ONLY webapi."CharacterLoadoutVisuals"
    ADD CONSTRAINT "CharacterLoadoutVisuals_loadout_config_key" UNIQUE (loadout_id, config_id);

ALTER TABLE ONLY webapi."CharacterLoadoutVisuals"
    ADD CONSTRAINT "CharacterLoadoutVisuals_pkey" PRIMARY KEY (id);

ALTER TABLE ONLY webapi."CharacterLoadouts"
    ADD CONSTRAINT "CharacterLoadouts_pkey" PRIMARY KEY (loadout_id);

ALTER TABLE ONLY webapi."CharacterResources"
    ADD CONSTRAINT "CharacterResources_character_guid_sdb_id_key" UNIQUE (character_guid, sdb_id);

ALTER TABLE ONLY webapi."CharacterResources"
    ADD CONSTRAINT "CharacterResources_pkey" PRIMARY KEY (id);

ALTER TABLE ONLY webapi."CharacterUnlocks"
    ADD CONSTRAINT "CharacterUnlocks_pkey" PRIMARY KEY (character_guid, unlock_type, unlock_id, frame_id);

ALTER TABLE ONLY webapi."NewCharacterTemplateBattleframes"
    ADD CONSTRAINT "NewCharacterTemplateBattleframes_pkey" PRIMARY KEY (template_id, battleframe_sdb_id);

ALTER TABLE ONLY webapi."NewCharacterTemplateItemModules"
    ADD CONSTRAINT "NewCharacterTemplateItemModules_pkey" PRIMARY KEY (template_id, template_item_id, slot_index);

ALTER TABLE ONLY webapi."NewCharacterTemplateItems"
    ADD CONSTRAINT "NewCharacterTemplateItems_pkey" PRIMARY KEY (template_id, template_item_id);

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutPerks"
    ADD CONSTRAINT "NewCharacterTemplateLoadoutPerks_pkey" PRIMARY KEY (template_id, template_loadout_id, perk_item_sdb_id);

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutSlots"
    ADD CONSTRAINT "NewCharacterTemplateLoadoutSlots_pkey" PRIMARY KEY (template_id, template_loadout_id, config_id, slot_index);

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutVisuals"
    ADD CONSTRAINT "NewCharacterTemplateLoadoutVisuals_pkey" PRIMARY KEY (template_id, template_loadout_id, config_id);

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadouts"
    ADD CONSTRAINT "NewCharacterTemplateLoadouts_pkey" PRIMARY KEY (template_id, template_loadout_id);

ALTER TABLE ONLY webapi."NewCharacterTemplateResources"
    ADD CONSTRAINT "NewCharacterTemplateResources_pkey" PRIMARY KEY (template_id, sdb_id);

ALTER TABLE ONLY webapi."NewCharacterTemplates"
    ADD CONSTRAINT "NewCharacterTemplates_name_key" UNIQUE (name);

ALTER TABLE ONLY webapi."NewCharacterTemplates"
    ADD CONSTRAINT "NewCharacterTemplates_pkey" PRIMARY KEY (template_id);

ALTER TABLE ONLY webapi."BattleframeAvailableUpgrades"
    ADD CONSTRAINT battleframe_available_upgrades_pkey PRIMARY KEY (battleframe_guid, upgrade_index);

ALTER TABLE ONLY webapi."BattleframePreviousUpgrades"
    ADD CONSTRAINT battleframe_previous_upgrades_pkey PRIMARY KEY (battleframe_guid, upgrade_id);

ALTER TABLE ONLY webapi."CharacterLoadoutPerks"
    ADD CONSTRAINT "CharacterLoadoutPerks_loadout_id_fkey" FOREIGN KEY (loadout_id) REFERENCES webapi."CharacterLoadouts"(loadout_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."CharacterLoadoutVisuals"
    ADD CONSTRAINT "CharacterLoadoutVisuals_loadout_id_fkey" FOREIGN KEY (loadout_id) REFERENCES webapi."CharacterLoadouts"(loadout_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."BattleframeAvailableUpgrades"
    ADD CONSTRAINT battleframe_available_upgrades_battleframe_guid_fkey FOREIGN KEY (battleframe_guid) REFERENCES webapi."Battleframes"(id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."BattleframePreviousUpgrades"
    ADD CONSTRAINT battleframe_previous_upgrades_battleframe_guid_fkey FOREIGN KEY (battleframe_guid) REFERENCES webapi."Battleframes"(id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."CharacterItemModules"
    ADD CONSTRAINT character_item_modules_item_guid_fkey FOREIGN KEY (item_guid) REFERENCES webapi."CharacterItems"(item_guid) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."CharacterItems"
    ADD CONSTRAINT character_items_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."CharacterItems"
    ADD CONSTRAINT character_items_loadout_id_fkey FOREIGN KEY (loadout_id) REFERENCES webapi."CharacterLoadouts"(loadout_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."CharacterLoadoutSlots"
    ADD CONSTRAINT character_loadout_slots_item_guid_fkey FOREIGN KEY (item_guid) REFERENCES webapi."CharacterItems"(item_guid) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."CharacterLoadoutSlots"
    ADD CONSTRAINT character_loadout_slots_loadout_id_fkey FOREIGN KEY (loadout_id) REFERENCES webapi."CharacterLoadouts"(loadout_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."CharacterLoadouts"
    ADD CONSTRAINT character_loadouts_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."CharacterResources"
    ADD CONSTRAINT character_resources_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."CharacterUnlocks"
    ADD CONSTRAINT character_unlocks_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."NewCharacterTemplateBattleframes"
    ADD CONSTRAINT template_battleframes_template_fkey FOREIGN KEY (template_id) REFERENCES webapi."NewCharacterTemplates"(template_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."NewCharacterTemplateItemModules"
    ADD CONSTRAINT template_item_modules_item_fkey FOREIGN KEY (template_id, template_item_id) REFERENCES webapi."NewCharacterTemplateItems"(template_id, template_item_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."NewCharacterTemplateItems"
    ADD CONSTRAINT template_items_template_fkey FOREIGN KEY (template_id) REFERENCES webapi."NewCharacterTemplates"(template_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutPerks"
    ADD CONSTRAINT template_loadout_perks_loadout_fkey FOREIGN KEY (template_id, template_loadout_id) REFERENCES webapi."NewCharacterTemplateLoadouts"(template_id, template_loadout_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutSlots"
    ADD CONSTRAINT template_loadout_slots_item_fkey FOREIGN KEY (template_id, template_item_id) REFERENCES webapi."NewCharacterTemplateItems"(template_id, template_item_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutSlots"
    ADD CONSTRAINT template_loadout_slots_loadout_fkey FOREIGN KEY (template_id, template_loadout_id) REFERENCES webapi."NewCharacterTemplateLoadouts"(template_id, template_loadout_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadoutVisuals"
    ADD CONSTRAINT template_loadout_visuals_loadout_fkey FOREIGN KEY (template_id, template_loadout_id) REFERENCES webapi."NewCharacterTemplateLoadouts"(template_id, template_loadout_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."NewCharacterTemplateLoadouts"
    ADD CONSTRAINT template_loadouts_template_fkey FOREIGN KEY (template_id) REFERENCES webapi."NewCharacterTemplates"(template_id) ON DELETE CASCADE;

ALTER TABLE ONLY webapi."NewCharacterTemplateResources"
    ADD CONSTRAINT template_resources_template_fkey FOREIGN KEY (template_id) REFERENCES webapi."NewCharacterTemplates"(template_id) ON DELETE CASCADE;

-- ---------------------------------------------------------------------------
-- Indexes
-- ---------------------------------------------------------------------------

CREATE UNIQUE INDEX IF NOT EXISTS accounts_login_ticket_hash_uindex ON webapi."Accounts" USING btree (login_ticket_hash);

CREATE UNIQUE INDEX IF NOT EXISTS character_items_loadout_id_uindex ON webapi."CharacterItems" USING btree (loadout_id) WHERE (loadout_id IS NOT NULL);

CREATE INDEX IF NOT EXISTS idx_character_unlocks_character ON webapi."CharacterUnlocks" USING btree (character_guid);

CREATE INDEX IF NOT EXISTS idx_charitems_char ON webapi."CharacterItems" USING btree (character_guid);

CREATE INDEX IF NOT EXISTS idx_charloadouts_char ON webapi."CharacterLoadouts" USING btree (character_guid);

CREATE UNIQUE INDEX IF NOT EXISTS new_character_templates_single_default ON webapi."NewCharacterTemplates" USING btree (is_default) WHERE is_default;

-- CreateNewCharacter takes a template now; drop the old signature so it does not
-- sit next to the new one as an overload.
DROP FUNCTION IF EXISTS webapi."CreateNewCharacter"(bigint, text, boolean, integer,
                                                   integer, bytea, OUT text, OUT bigint);

-- ---------------------------------------------------------------------------
-- Functions
-- ---------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION webapi."CreateNewCharacter"(account_id bigint, name text, is_dev boolean, voice_setid integer, gender integer, visuals bytea, template_id integer DEFAULT 0, OUT error_text text, OUT new_character_id bigint) RETURNS record
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

CREATE OR REPLACE FUNCTION webapi."InvalidateLoginTicketOnPasswordChange"() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.login_ticket            := NULL;
    NEW.login_ticket_hash       := NULL;
    NEW.login_ticket_created_at := NULL;
    RETURN NEW;
END
$$;

CREATE OR REPLACE FUNCTION webapi.build_template_from_char_create(p_template_name text, p_battleframe_level integer DEFAULT 1, p_frames integer[] DEFAULT NULL::integer[], p_allow_pvp_fallback boolean DEFAULT false) RETURNS integer
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

CREATE OR REPLACE FUNCTION webapi.create_chassis_item_for_loadout() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO webapi."CharacterItems" (item_guid, character_guid, sdb_id, sub_inventory, loadout_id)
    VALUES (webapi.create_entity_guid(253), NEW.character_guid, NEW.chassis_id, 4, NEW.loadout_id)
    ON CONFLICT DO NOTHING;

    RETURN NEW;
END
$$;

CREATE OR REPLACE FUNCTION webapi.notify_on_unlock_events() RETURNS trigger
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

CREATE OR REPLACE FUNCTION webapi.seed_character_inventory(char_guid bigint, p_template_id integer DEFAULT 0) RETURNS void
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

    -- The set is copied whole, jsonb and all: order and float values are the data, and re-shaping either
    -- here would change what the player sees.
    INSERT INTO webapi."CharacterLoadoutVisuals" (loadout_id, config_id, visuals)
    SELECT lm.loadout_id, v.config_id, v.visuals
    FROM webapi."NewCharacterTemplateLoadoutVisuals" v
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

CREATE OR REPLACE FUNCTION webapi.snapshot_inventory_as_template(char_guid bigint, p_template_id integer) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM webapi."NewCharacterTemplates" t WHERE t.template_id = p_template_id) THEN
        RAISE EXCEPTION 'snapshot_inventory_as_template: no template %', p_template_id;
    END IF;

    DELETE FROM webapi."NewCharacterTemplateLoadoutVisuals" WHERE template_id = p_template_id;
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

    INSERT INTO webapi."NewCharacterTemplateLoadoutVisuals" (template_id, template_loadout_id, config_id, visuals)
    SELECT p_template_id, lm.template_loadout_id, v.config_id, v.visuals
    FROM webapi."CharacterLoadoutVisuals" v
    JOIN (SELECT loadout_id, row_number() OVER (ORDER BY loadout_id) AS template_loadout_id
          FROM webapi."CharacterLoadouts" WHERE character_guid = char_guid) lm USING (loadout_id);

    INSERT INTO webapi."NewCharacterTemplateBattleframes" (template_id, battleframe_sdb_id, level, elite_level, elite_points, hidden, visuals, bandwidth)
    SELECT p_template_id, battleframe_sdb_id, level, elite_level, elite_points, hidden, visuals, bandwidth
    FROM webapi."Battleframes"
    WHERE character_guid = char_guid;
END
$$;

ALTER FUNCTION webapi."InvalidateLoginTicketOnPasswordChange"() OWNER TO tmwadmin;

ALTER FUNCTION webapi.build_template_from_char_create(p_template_name text, p_battleframe_level integer, p_frames integer[], p_allow_pvp_fallback boolean) OWNER TO tmwadmin;

COMMENT ON FUNCTION webapi.build_template_from_char_create(p_template_name text, p_battleframe_level integer, p_frames integer[], p_allow_pvp_fallback boolean) IS 'Builds a seeding template from sdb dbcharacter::CharCreateLoadout. Slots above LoadoutSlot.level_requirement are skipped, as are slots with a zero default_pve_module. Bandwidth is set from cumulative dbitems::FrameProgressionLevel.perk_points_gain. No perks are seeded: CharCreateLoadoutPerks is an unlock list, not an equipped set.';

ALTER FUNCTION webapi.create_chassis_item_for_loadout() OWNER TO tmwadmin;

ALTER FUNCTION webapi.notify_on_unlock_events() OWNER TO tmwadmin;

ALTER FUNCTION webapi.seed_character_inventory(char_guid bigint, p_template_id integer) OWNER TO tmwadmin;

ALTER FUNCTION webapi.snapshot_inventory_as_template(char_guid bigint, p_template_id integer) OWNER TO tmwadmin;

-- ---------------------------------------------------------------------------
-- Triggers
-- ---------------------------------------------------------------------------

DROP TRIGGER IF EXISTS accounts_password_change_invalidates_ticket ON webapi."Accounts";

CREATE TRIGGER accounts_password_change_invalidates_ticket BEFORE UPDATE OF password_hash, secret ON webapi."Accounts" FOR EACH ROW WHEN (((old.password_hash IS DISTINCT FROM new.password_hash) OR (old.secret IS DISTINCT FROM new.secret))) EXECUTE FUNCTION webapi."InvalidateLoginTicketOnPasswordChange"();

DROP TRIGGER IF EXISTS character_unlock_events_trigger ON webapi."CharacterUnlocks";

CREATE TRIGGER character_unlock_events_trigger AFTER INSERT OR DELETE ON webapi."CharacterUnlocks" FOR EACH ROW EXECUTE FUNCTION webapi.notify_on_unlock_events();

DROP TRIGGER IF EXISTS loadout_chassis_item_trigger ON webapi."CharacterLoadouts";

CREATE TRIGGER loadout_chassis_item_trigger AFTER INSERT ON webapi."CharacterLoadouts" FOR EACH ROW WHEN ((new.loadout_type = 'battleframe'::text)) EXECUTE FUNCTION webapi.create_chassis_item_for_loadout();

-- ---------------------------------------------------------------------------
-- Column and table comments
-- ---------------------------------------------------------------------------

COMMENT ON COLUMN webapi."Accounts".login_ticket IS 'The 32 random bytes behind this account''s login ticket, after the "R5 " header is stripped and before base64. The oracle/ticket route hands the client base64("R5 " + these bytes); the client gives the game server the decoded payload and the game server passes those bytes back for validation.';

COMMENT ON COLUMN webapi."Accounts".login_ticket_hash IS 'SHA-256 of login_ticket. Validation hashes the presented ticket and looks this up, so the comparison never touches the raw column. Unique: two accounts must never accept the same ticket.';

COMMENT ON COLUMN webapi."Accounts".login_ticket_created_at IS 'When the current ticket was minted. Cleared together with the ticket when the password or signing secret changes.';

COMMENT ON COLUMN webapi."Battleframes".bandwidth IS 'Perk point budget for this battleframe, shared by all of its loadouts. PIN computes it on level up and writes it; RIN stores it and never validates perk totals or the 10 perk cap against it.';

COMMENT ON COLUMN webapi."CharacterItems".loadout_id IS 'Set on the one item that is a loadout''s chassis: the item the client API names as a garage slot''s item_guid. NULL on ordinary items. Unique where set, so a loadout has exactly one chassis, and ON DELETE CASCADE so the item goes with the loadout.';

COMMENT ON TABLE webapi."CharacterLoadoutPerks" IS 'Perk modules the player has slotted into a loadout. Identified by sdb id (dbitems::PerkModule.id), not by guid. PIN enforces the 10 perk cap and the frame bandwidth.';

COMMENT ON TABLE webapi."CharacterLoadoutVisuals" IS 'The whole visual set of one loadout config: palettes, patterns, decals, glider, vehicle. PIN owns the set and sends it whole; RIN stores it verbatim and validates nothing about it.';

COMMENT ON COLUMN webapi."CharacterLoadoutVisuals".config_id IS '0 PvE, 1 PvP. Same numbering as CharacterLoadoutSlots.config_id. An absent row means the config has no visuals and the battleframe falls back to its dbitems::Battleframe defaults.';

COMMENT ON COLUMN webapi."CharacterLoadoutVisuals".visuals IS 'Ordered JSON array of {visualType, itemSdbId, data1, data2, transform}. visualType 9 palette, 10 pattern, 11 decal, 13 glider, 14 vehicle. Pattern transform is 4 floats (degrees, scale, h offset, v offset), decal transform is 12 (3x3 rotation+scale row major, then translation). Stored as the client sent them: no rounding, no clamping, no reordering.';

COMMENT ON COLUMN webapi."NewCharacterTemplateBattleframes".visuals IS 'Serialised PlayerBattleframeVisuals. The default is PlayerBattleframeVisuals.CreateDefault().';

COMMENT ON TABLE webapi."NewCharacterTemplateLoadoutVisuals" IS 'Visual sets a seeded character starts with. snapshot_inventory_as_template carries them; build_template_from_char_create cannot, because sdb dbcharacter::CharCreateLoadout has no warpaint columns.';

COMMENT ON COLUMN webapi."NewCharacterTemplates".is_default IS 'The template used when template_id 0 is passed, i.e. what CreateNewCharacter and SeedCharacterFromTemplate seed from. Exactly one row may have it.';

-- ---------------------------------------------------------------------------
-- Chassis items for loadouts that already exist
-- ---------------------------------------------------------------------------

INSERT INTO webapi."CharacterItems" (item_guid, character_guid, sdb_id, sub_inventory, loadout_id)
SELECT webapi.create_entity_guid(253), l.character_guid, l.chassis_id, 4, l.loadout_id
FROM webapi."CharacterLoadouts" l
WHERE l.loadout_type = 'battleframe'
  AND NOT EXISTS (SELECT 1 FROM webapi."CharacterItems" ci WHERE ci.loadout_id = l.loadout_id)
ON CONFLICT DO NOTHING;

COMMIT;
