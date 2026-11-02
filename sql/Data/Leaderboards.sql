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
-- Name: Leaderboards_id_seq; Type: SEQUENCE SET; Schema: webapi; Owner: tmwadmin
--

SELECT pg_catalog.setval('webapi."Leaderboards_id_seq"', 1, false);


--
-- Data for Name: Leaderboards; Type: TABLE DATA; Schema: webapi; Owner: tmwadmin
--

INSERT INTO webapi."Leaderboards" (id, name, type, "order")
VALUES (1, 'Copacabana - Trans-Hub race', 1, 0),
       (2, 'Trans-Hub - Sunken Harbor race', 1, 0),
       (3, 'Sunken Harbor - Copacabana race', 1, 0),
       (4, 'Copacabana - Thump Dump race', 1, 0),
       (5, 'Thump Dump - Copacabana race', 1, 0),
       (6, 'Shore Shot - Northern Shores Race', 1, 0),
       (7, 'Cerrado Challenge - Cerrado Race', 1, 0),
       (8, 'Nutretic Loop - Nutretic Race', 1, 0),
       (9, 'Dredge Drive - Dredge Race', 1, 0),
       (10, 'Desert Drop Off - Station South-east of Andreev Station', 1, 0),
       (11, 'Redwood Roundabout - Lab 16 Race', 1, 0),
       (12, 'Devils Eight - Crossroads Station Race', 1, 0),
       (13, 'Jungle Jumps - Forest Watch Race', 1, 0),
       (14, 'Lavaventure - Camp Jasper Race', 1, 0)
;
