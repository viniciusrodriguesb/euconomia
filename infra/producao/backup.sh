#!/usr/bin/env bash
# As variáveis dos comandos SQL são expandidas dentro do container.
# shellcheck disable=SC2016
set -Eeuo pipefail
umask 077
source "$(dirname -- "${BASH_SOURCE[0]}")/comum.sh"

mkdir -p "$EUCONOMIA_DIR/backups"
destino="$EUCONOMIA_DIR/backups/euconomia-$(date -u +%Y%m%dT%H%M%S)-$$.dump"
temporario="$destino.part"
trap 'rm -f -- "$temporario"' EXIT

compose exec -T postgres sh -ec \
  'pg_dump --username "$EUCONOMIA_DB_USER" --dbname "$POSTGRES_DB" --format=custom --no-owner' > "$temporario"
compose exec -T postgres pg_restore --list < "$temporario" > /dev/null
mv -- "$temporario" "$destino"
printf '%s\n' "$destino"
