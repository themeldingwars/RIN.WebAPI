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
       (5, 'Thump Dump - Copacabana race', 1, 0);
