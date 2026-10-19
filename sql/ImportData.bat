rem NewCharacterTemplate.sql must be loaded or CreateNewCharacter seeds an empty inventory.
(docker exec -i RIN.WebAPI.DB psql -U tmwadmin -s -v ON_ERROR_STOP=1 TMW) < Data/NewCharacterTemplate.sql
(docker exec -i RIN.WebAPI.DB psql -U tmwadmin -s -v ON_ERROR_STOP=1 TMW) < Data/ZoneSettings.sql
(docker exec -i RIN.WebAPI.DB psql -U tmwadmin -s -v ON_ERROR_STOP=1 TMW) < Data/ZoneCertificates.sql
(docker exec -i RIN.WebAPI.DB psql -U tmwadmin -s -v ON_ERROR_STOP=1 TMW) < Data/ZoneDifficulty.sql
(docker exec -i RIN.WebAPI.DB psql -U tmwadmin -s -v ON_ERROR_STOP=1 TMW) < Data/Leaderboards.sql
