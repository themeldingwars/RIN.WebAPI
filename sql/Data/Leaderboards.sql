--
-- Data for Name: Leaderboards; Type: TABLE DATA; Schema: webapi; Owner: tmwadmin
--

INSERT INTO webapi."Leaderboards" VALUES (1, 'Copacabana - Trans-Hub race', 1, 0);
INSERT INTO webapi."Leaderboards" VALUES (2, 'Trans-Hub - Sunken Harbor race', 1, 0);
INSERT INTO webapi."Leaderboards" VALUES (3, 'Sunken Harbor - Copacabana race', 1, 0);
INSERT INTO webapi."Leaderboards" VALUES (4, 'Copacabana - Thump Dump race', 1, 0);
INSERT INTO webapi."Leaderboards" VALUES (5, 'Thump Dump - Copacabana race', 1, 0);

--
-- Name: Leaderboards_id_seq; Type: SEQUENCE SET; Schema: webapi; Owner: tmwadmin
--

SELECT pg_catalog.setval('webapi."Leaderboards_id_seq"', 1, true);
