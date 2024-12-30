(docker exec -i RIN.WebAPI.DB psql -U tmwadmin -s TMW) < Data/ZoneSettings.sql
(docker exec -i RIN.WebAPI.DB psql -U tmwadmin -s TMW) < Data/ZoneCertificates.sql
(docker exec -i RIN.WebAPI.DB psql -U tmwadmin -s TMW) < Data/ZoneDifficulty.sql
(docker exec -i RIN.WebAPI.DB psql -U tmwadmin -s TMW) < Data/Leaderboards.sql
