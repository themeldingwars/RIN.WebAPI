--
-- Leaderboards for the remaining time-trial races.
--
-- Safe to run against an existing database, and safe to run twice.

BEGIN;

INSERT INTO webapi."Leaderboards" (id, name, type, "order")
VALUES (6, 'Shore Shot - Northern Shores Race', 1, 0),
       (7, 'Cerrado Challenge - Cerrado Race', 1, 0),
       (8, 'Nutretic Loop - Nutretic Race', 1, 0),
       (9, 'Dredge Drive - Dredge Race', 1, 0),
       (10, 'Desert Drop Off - Station South-east of Andreev Station', 1, 0),
       (11, 'Redwood Roundabout - Lab 16 Race', 1, 0),
       (12, 'Devils Eight - Crossroads Station Race', 1, 0),
       (13, 'Jungle Jumps - Forest Watch Race', 1, 0),
       (14, 'Lavaventure - Camp Jasper Race', 1, 0)
ON CONFLICT (id) DO NOTHING;

COMMIT;
