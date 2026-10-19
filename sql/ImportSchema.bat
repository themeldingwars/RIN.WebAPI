rem webapi_schema.sql is a pg_dump -s dump with no DROP statements, so this only works on an
rem empty database. To rebuild an existing one, drop the schema first:
rem   docker exec -i RIN.WebAPI.DB psql -U tmwadmin -d TMW -c "DROP SCHEMA webapi CASCADE;"
rem or use sql/import.sh, which does the backup, drop, create and data load in one go.
(docker exec -i RIN.WebAPI.DB psql -U tmwadmin -s -v ON_ERROR_STOP=1 TMW) < webapi_schema.sql
