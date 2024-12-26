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
-- TOC entry 5187 (class 0 OID 0)
-- Dependencies: 812
-- Name: ZoneSettings_id_seq; Type: SEQUENCE SET; Schema: webapi; Owner: tmwadmin
--

SELECT pg_catalog.setval('webapi."ZoneSettings_id_seq"', 1, false);


--
-- TOC entry 5181 (class 0 OID 19539)
-- Dependencies: 813
-- Data for Name: ZoneSettings; Type: TABLE DATA; Schema: webapi; Owner: tmwadmin
--

INSERT INTO webapi."ZoneSettings" (id, zone_id,mission_id,gametype,instance_type_pool,is_preview_zone,displayed_name,displayed_desc,description,displayed_gametype,cert_required,xp_bonus,sort_order,rotation_priority,skip_matchmaking,queueing_enabled,team_count,min_players_per_team,max_players_per_team,min_players_accept_per_team,challenge_enabled,challenge_min_players_per_team,challenge_max_players_per_team,is_active,images) VALUES
	 (1,1051,NULL,'raid','pve',false,'MATCH_MAP_BOSS_BANECLAW','MATCH_MAP_BOSS_BANECLAW_DESC','Baneclaw','MATCH_MAP_BOSS_BANECLAW',false,0,NULL,1,true,true,1,10,20,0,false,0,0,true,'{"thumbnail":"/assets/zones/baneclaw/tbn.png","screenshot":["/assets/zones/baneclaw/01.png","/assets/zones/baneclaw/02.png","/assets/zones/baneclaw/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}'),
	 (2,1054,NULL,'raid','pve',false,'DT_WARFRONT','','Devils Tusk Warfront','DT_WARFRONT',false,0,NULL,1,true,true,1,10,20,0,false,10,20,false,'{"thumbnail":"/assets/zones/placeholder-tbn.png","screenshot":["/assets/zones/placeholder-ss.png","/assets/zones/placeholder-ss.png","/assets/zones/placeholder-ss.png"],"lfg":"/assets/zones/placeholder-ss.png"}'),
	 (3,1028,NULL,'intro','pve',false,'DIAMONDHEAD_INTRO','','Diamondhead Intro','DIAMONDHEAD_INTRO',false,0,NULL,1,true,true,1,1,5,0,false,1,5,true,'{"thumbnail":"/assets/zones/placeholder-tbn.png","screenshot":["/assets/zones/placeholder-ss.png","/assets/zones/placeholder-ss.png","/assets/zones/placeholder-ss.png"],"lfg":"/assets/zones/placeholder-ss.png"}'),
	 (4,863,NULL,'mission','pve',false,'MATCH_MAP_JUNGLE_CLIFFS','JUNGLE_CLIFFS_QUEUE','Jungle Cliffs','INSTANCE_QUEUE_GAMETYPE_ARES',false,0,NULL,1,true,true,1,1,5,0,false,0,0,false,'{"thumbnail":"/assets/zones/jungle_cliffs/tbn.png","screenshot":["/assets/zones/jungle_cliffs/01.png","/assets/zones/jungle_cliffs/02.png","/assets/zones/jungle_cliffs/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}'),
	 (5,1022,NULL,'raid','pve',false,'MATCH_MAP_BOSS_KANALOA','MATCH_MAP_BOSS_KANALOA_DESC','Kanaloa','MATCH_MAP_BOSS_KANALOA',false,0,NULL,1,true,true,1,10,20,0,false,0,0,false,'{"thumbnail":"/assets/zones/kanaloa/tbn.png","screenshot":["/assets/zones/kanaloa/01.png","/assets/zones/kanaloa/02.png","/assets/zones/kanaloa/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}'),
	 (6,833,NULL,'meldingfragment','pve',false,'MATCH_MAP_BLACKWATER_ANOMALY','','Melding Fragment Blackwater Anomaly','INSTANCE_QUEUE_GAMETYPE_MELDING',true,0,NULL,0,true,true,1,2,5,0,false,0,0,true,'{"thumbnail":"/assets/zones/blackwater_anomaly/tbn.png","screenshot":["/assets/zones/blackwater_anomaly/01.png","/assets/zones/blackwater_anomaly/02.png","/assets/zones/blackwater_anomaly/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}'),
	 (7,1069,NULL,'mission','pve',false,'MATCH_MAP_OPERATION_MIRU','OPERATION_MIRU_QUEUE','Operation Miru','INSTANCE_QUEUE_GAMETYPE_ARES',false,0,NULL,1,true,true,1,2,5,0,false,2,5,true,'{"thumbnail":"/assets/zones/operation_miru/tbn.png","screenshot":["/assets/zones/operation_miru/01.png","/assets/zones/operation_miru/02.png","/assets/zones/operation_miru/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}'),
	 (8,1003,495,'campaign','pve',false,'MATCH_MAP_HARVESTER_ISLAND','MATCH_MAP_HARVESTER_ISLAND_DESC','Chapter 1: Crash Down','MATCH_MAP_HARVESTER_ISLAND',false,0,1,1,false,true,1,1,5,0,false,0,0,true,'{"thumbnail":"/assets/zones/blackwater/tbn.png","screenshot":["/assets/zones/blackwater/01.png","/assets/zones/blackwater/02.png","/assets/zones/blackwater/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}'),
	 (9,864,491,'campaign','pve',false,'MATCH_MAP_BANDIT_CAVE','MATCH_MAP_BANDIT_CAVE_DESC','Chapter 1: Bandit Cave','INSTANCE_QUEUE_GAMETYPE_ARES',false,0,2,1,false,true,1,1,5,0,false,0,0,true,'{"thumbnail":"/assets/zones/tanken_caverns/tbn.png","screenshot":["/assets/zones/tanken_caverns/01.png","/assets/zones/tanken_caverns/02.png","/assets/zones/tanken_caverns/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}'),
	 (10,861,497,'campaign','pve',false,'MATCH_MAP_PROVING_GROUND','MATCH_MAP_PROVING_GROUND_DESC','Chapter 1: Proving Ground','MATCH_MAP_PROVING_GROUND',false,0,3,1,false,true,1,1,5,0,false,0,0,false,'{"thumbnail":"/assets/zones/proving_ground/tbn.png","screenshot":["/assets/zones/proving_ground/01.png","/assets/zones/proving_ground/02.png","/assets/zones/proving_ground/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}');

INSERT INTO webapi."ZoneSettings" (id, zone_id,mission_id,gametype,instance_type_pool,is_preview_zone,displayed_name,displayed_desc,description,displayed_gametype,cert_required,xp_bonus,sort_order,rotation_priority,skip_matchmaking,queueing_enabled,team_count,min_players_per_team,max_players_per_team,min_players_accept_per_team,challenge_enabled,challenge_min_players_per_team,challenge_max_players_per_team,is_active,images) VALUES
	 (11,1007,502,'campaign','pve',false,'MATCH_MAP_POWER_GRAB','MATCH_MAP_POWER_GRAB_DESC','Chapter 1: Power Grab','MATCH_MAP_POWER_GRAB',false,0,4,1,false,true,1,1,5,0,false,1,5,true,'{"thumbnail":"/assets/zones/power_grab/tbn.png","screenshot":["/assets/zones/power_grab/01.png","/assets/zones/power_grab/02.png","/assets/zones/power_grab/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}'),
	 (12,1008,506,'campaign','pve',false,'MATCH_MAP_RISKY_BUSINESS','MATCH_MAP_RISKY_BUSINESS_DESC','Chapter 1: Risky Business','MATCH_MAP_RISKY_BUSINESS',false,0,5,1,false,true,1,1,5,0,false,0,0,true,'{"thumbnail":"/assets/zones/risky_business/tbn.png","screenshot":["/assets/zones/risky_business/01.png","/assets/zones/risky_business/02.png","/assets/zones/risky_business/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}'),
	 (13,833,507,'campaign','pve',false,'MATCH_MAP_BLACKWATER_ANOMALY','MATCH_MAP_BLACKWATER_ANOMALY_DESC','Chapter 1: Blackwater Anomaly','MATCH_MAP_BLACKWATER_ANOMALY',false,0,6,1,false,true,1,1,5,0,false,0,0,true,'{"thumbnail":"/assets/zones/blackwater_anomaly/tbn.png","screenshot":["/assets/zones/blackwater_anomaly/01.png","/assets/zones/blackwater_anomaly/02.png","/assets/zones/blackwater_anomaly/03.png"],"lfg":"/assets/zones/placeholder-ss.png"}');


-- Completed on 2023-12-30 20:33:24 UTC

--
-- PostgreSQL database dump complete
--

