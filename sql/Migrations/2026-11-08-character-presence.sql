--
-- Where an online character is now, as the game servers report it. The web API's army roster shows it; NULL means
-- the character is offline. last_zone_id stays the zone a character logs back in to, which is never an instance.
--
-- Safe to run against an existing database, and safe to run twice.

BEGIN;

ALTER TABLE webapi."Characters" ADD COLUMN IF NOT EXISTS current_zone_id integer;

COMMIT;
