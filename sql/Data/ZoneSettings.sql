--
-- PostgreSQL database dump
--

-- Dumped from database version 16.1
-- Dumped by pg_dump version 16.1

-- Started on 2023-12-30 20:33:24 UTC

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
-- TOC entry 5181 (class 0 OID 19539)
-- Dependencies: 813
-- Data for Name: ZoneSettings; Type: TABLE DATA; Schema: webapi; Owner: tmwadmin
--

INSERT INTO webapi."ZoneSettings" VALUES (1, 833, 507, 'campaign', 'pve', false, 'MATCH_MAP_BLACKWATER_ANOMALY', 'MATCH_MAP_BLACKWATER_ANOMALY_DESC', 'Chapter 1: Blackwater Anomaly', 'MATCH_MAP_BLACKWATER_ANOMALY', false, 0, 6, 1, false, true, 1, 1, 5, 0, false, 0, 0, false, '{"thumbnail":"/assets/zones/blackwater_anomaly/tbn.png","screenshot":["/assets/zones/blackwater_anomaly/01.png","/assets/zones/blackwater_anomaly/02.png","/assets/zones/blackwater_anomaly/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}');


--
-- TOC entry 5187 (class 0 OID 0)
-- Dependencies: 812
-- Name: ZoneSettings_id_seq; Type: SEQUENCE SET; Schema: webapi; Owner: tmwadmin
--

SELECT pg_catalog.setval('webapi."ZoneSettings_id_seq"', 1, true);


-- Completed on 2023-12-30 20:33:24 UTC

--
-- PostgreSQL database dump complete
--

