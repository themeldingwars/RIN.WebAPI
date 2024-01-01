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
-- TOC entry 5181 (class 0 OID 19564)
-- Dependencies: 815
-- Data for Name: ZoneDifficulty; Type: TABLE DATA; Schema: webapi; Owner: tmwadmin
--

INSERT INTO webapi."ZoneDifficulty" VALUES (1, 1, 'NORMAL_MODE', 'INSTANCE_DIFFICULTY_NORMAL', 24, 24, 40, 5, 5, 5, 1, 5);
INSERT INTO webapi."ZoneDifficulty" VALUES (2, 1, 'HARD_MODE', 'INSTANCE_DIFFICULTY_HARD', 37, 37, 40, 5, 5, 5, 1, 5);


--
-- TOC entry 5187 (class 0 OID 0)
-- Dependencies: 814
-- Name: ZoneDifficulty_id_seq; Type: SEQUENCE SET; Schema: webapi; Owner: tmwadmin
--

SELECT pg_catalog.setval('webapi."ZoneDifficulty_id_seq"', 2, true);


-- Completed on 2023-12-30 20:34:05 UTC

--
-- PostgreSQL database dump complete
--

