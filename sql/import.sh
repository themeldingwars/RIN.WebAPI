#!/usr/bin/env bash
#
# Rebuild the local webapi schema from webapi_schema.sql and reload the reference data.
#
#   ./import.sh              backup, drop, recreate, load data
#   ./import.sh --no-backup  skip the pg_dump safety net
#
# Only the webapi schema is touched - the sdb schema is left alone.
# Override the defaults with CONTAINER / DB_NAME / DB_USER if your setup differs.
#
set -euo pipefail

CONTAINER="${CONTAINER:-RIN.WebAPI.DB}"
DB_NAME="${DB_NAME:-TMW}"
DB_USER="${DB_USER:-tmwadmin}"

cd "$(dirname "${BASH_SOURCE[0]}")"

DATA_FILES=(
    Data/NewCharacterTemplate.sql
    Data/ZoneSettings.sql
    Data/ZoneCertificates.sql
    Data/ZoneDifficulty.sql
    Data/Leaderboards.sql
)

psql_run() { docker exec -i "$CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -v ON_ERROR_STOP=1 "$@"; }

if [[ "${1:-}" != "--no-backup" ]]; then
    if docker exec -i "$CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -At \
           -c "SELECT 1 FROM information_schema.schemata WHERE schema_name = 'webapi'" | grep -q 1; then
        backup="webapi_backup_$(date +%F_%H%M).sql"
        echo "Backing up webapi -> $backup"
        docker exec -i "$CONTAINER" pg_dump -U "$DB_USER" -d "$DB_NAME" -n webapi > "$backup"
    else
        echo "No webapi schema present - nothing to back up"
    fi
fi

echo "Dropping schema webapi"
psql_run -q -c 'DROP SCHEMA IF EXISTS webapi CASCADE;' > /dev/null

echo "Applying webapi_schema.sql"
psql_run -q < webapi_schema.sql > /dev/null

for file in "${DATA_FILES[@]}"; do
    echo "Loading $file"
    psql_run -q < "$file" > /dev/null
done

echo
psql_run -At -c "
SELECT 'tables='         || (SELECT count(*) FROM information_schema.tables
                              WHERE table_schema = 'webapi' AND table_type = 'BASE TABLE')
    || '  functions='    || (SELECT count(*) FROM pg_proc p
                              JOIN pg_namespace n ON n.oid = p.pronamespace
                             WHERE n.nspname = 'webapi')
    || '  template_items=' || (SELECT count(*) FROM webapi.\"NewCharacterTemplateItems\")
    || '  battleframe_columns=' || (SELECT count(*) FROM information_schema.columns
                                     WHERE table_schema = 'webapi' AND table_name = 'Battleframes');"
