--
-- PostgreSQL database dump
--

-- Dumped from database version 16.1
-- Dumped by pg_dump version 16.1

-- Started on 2023-12-30 20:34:05 UTC

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
-- TOC entry 5187 (class 0 OID 0)
-- Dependencies: 814
-- Name: ZoneDifficulty_id_seq; Type: SEQUENCE SET; Schema: webapi; Owner: tmwadmin
--

SELECT pg_catalog.setval('webapi."ZoneDifficulty_id_seq"', 1, false);


--
-- TOC entry 5181 (class 0 OID 19564)
-- Dependencies: 815
-- Data for Name: ZoneDifficulty; Type: TABLE DATA; Schema: webapi; Owner: tmwadmin
--

INSERT INTO webapi."ZoneDifficulty" (zone_setting_id,difficulty_key,ui_string,display_level,min_level,max_suggested_level,min_players,max_players,min_players_accept,group_min_players,group_max_players) VALUES
	 (1,'NORMAL_MODE','INSTANCE_DIFFICULTY_NORMAL',37,37,40,20,20,20,10,20),
	 (2,'NORMAL_MODE','INSTANCE_DIFFICULTY_NORMAL',40,40,40,20,20,20,10,20),
	 (3,'','INSTANCE_DIFFICULTY_NORMAL',37,37,40,5,5,5,1,5),
	 (4,'NORMAL_MODE','INSTANCE_DIFFICULTY_NORMAL',32,32,40,5,5,5,1,5),
	 (5,'NORMAL_MODE','INSTANCE_DIFFICULTY_NORMAL',39,39,40,20,20,20,10,20),
	 (5,'HARD_MODE','INSTANCE_DIFFICULTY_HARD',40,40,40,20,20,20,10,20),
	 (7,'NORMAL_MODE','INSTANCE_DIFFICULTY_NORMAL',37,37,40,5,5,5,2,5),
	 (7,'HARD_MODE','INSTANCE_DIFFICULTY_HARD',37,37,40,5,5,5,2,5),
	 (8,'HARD_MODE','INSTANCE_DIFFICULTY_HARD',37,37,40,5,5,5,1,5);

INSERT INTO webapi."ZoneDifficulty" (zone_setting_id,difficulty_key,ui_string,display_level,min_level,max_suggested_level,min_players,max_players,min_players_accept,group_min_players,group_max_players) VALUES
	 (8,'NORMAL_MODE','INSTANCE_DIFFICULTY_NORMAL',1,1,40,5,5,5,1,5),
	 (9,'NORMAL_MODE','INSTANCE_DIFFICULTY_NORMAL',12,12,40,5,5,5,1,5),
	 (9,'HARD_MODE','INSTANCE_DIFFICULTY_HARD',37,37,40,5,5,5,1,5),
	 (10,'NORMAL_MODE','INSTANCE_DIFFICULTY_NORMAL',15,15,40,5,5,5,1,5),
	 (10,'HARD_MODE','INSTANCE_DIFFICULTY_HARD',37,37,40,5,5,5,1,5),
	 (11,'NORMAL_MODE','INSTANCE_DIFFICULTY_NORMAL',18,18,40,5,5,5,1,5),
	 (11,'HARD_MODE','INSTANCE_DIFFICULTY_HARD',37,37,40,5,5,5,1,5),
	 (12,'NORMAL_MODE','INSTANCE_DIFFICULTY_NORMAL',21,21,40,5,5,5,1,5),
	 (12,'HARD_MODE','INSTANCE_DIFFICULTY_HARD',37,37,40,5,5,5,1,5),
     (13,'NORMAL_MODE','INSTANCE_DIFFICULTY_NORMAL',24,24,40,5,5,5,1,5),
     (13,'HARD_MODE','INSTANCE_DIFFICULTY_HARD',37,37,40,5,5,5,1,5);

-- Completed on 2023-12-30 20:34:05 UTC

--
-- PostgreSQL database dump complete
--

