--
-- PostgreSQL database dump
--

-- Dumped from database version 16.1
-- Dumped by pg_dump version 16.1

-- Started on 2023-12-30 20:34:24 UTC

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
-- TOC entry 5181 (class 0 OID 19573)
-- Dependencies: 817
-- Data for Name: ZoneCertificates; Type: TABLE DATA; Schema: webapi; Owner: tmwadmin
--

INSERT INTO webapi."ZoneCertificates" VALUES (1, 3592, 1, 'all', NULL, 'present');


--
-- TOC entry 5187 (class 0 OID 0)
-- Dependencies: 816
-- Name: ZoneCertificates_id_seq; Type: SEQUENCE SET; Schema: webapi; Owner: tmwadmin
--

SELECT pg_catalog.setval('webapi."ZoneCertificates_id_seq"', 1, true);


-- Completed on 2023-12-30 20:34:24 UTC

--
-- PostgreSQL database dump complete
--

