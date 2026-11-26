#!/usr/bin/env bash
set -Eeuo pipefail

psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  --set=ON_ERROR_STOP=1 --set=usuario="$EUCONOMIA_DB_USER" \
  --set=senha="$EUCONOMIA_DB_PASSWORD" --set=banco="$POSTGRES_DB" <<'SQL'
SELECT format('CREATE ROLE %I LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD %L', :'usuario', :'senha') \gexec
SELECT format('ALTER DATABASE %I OWNER TO %I', :'banco', :'usuario') \gexec
SQL
