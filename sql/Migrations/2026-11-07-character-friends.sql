--
-- Friends lists. Each character has its own row for every entry on its list, so a friendship is two rows, one each
-- way, added and removed together. A friendship starts as a request the other character has to accept. status is the
-- game client's status for the entry on character_guid's list:
--   1 friends
--   2 the owner of the list asked the friend and waits for an answer (the client calls it PENDING_IN)
--   3 the friend asked the owner, who has to accept or decline (PENDING_OUT, the client shows the invite)
-- note is the owner's own note on the friend, only the owner sees it.
--
-- Safe to run twice.

BEGIN;

CREATE TABLE IF NOT EXISTS webapi."CharacterFriends" (
    character_guid bigint NOT NULL,
    friend_guid bigint NOT NULL,
    status smallint NOT NULL,
    note text DEFAULT ''::text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT "CharacterFriends_pkey" PRIMARY KEY (character_guid, friend_guid),
    CONSTRAINT "CharacterFriends_self" CHECK ((character_guid <> friend_guid)),
    CONSTRAINT "CharacterFriends_status" CHECK (((status >= 1) AND (status <= 3))),
    CONSTRAINT character_friends_character_guid_fkey FOREIGN KEY (character_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE,
    CONSTRAINT character_friends_friend_guid_fkey FOREIGN KEY (friend_guid) REFERENCES webapi."Characters"(character_guid) ON DELETE CASCADE
);

ALTER TABLE webapi."CharacterFriends" OWNER TO tmwadmin;

-- Friends are looked up by name without case
CREATE INDEX IF NOT EXISTS idx_characters_upper_name ON webapi."Characters" USING btree (upper(name));

-- Serves the cascade when a friend is deleted, the primary key finds everything else
CREATE INDEX IF NOT EXISTS idx_character_friends_friend ON webapi."CharacterFriends" USING btree (friend_guid);

COMMIT;
