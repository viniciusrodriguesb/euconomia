#!/usr/bin/env bash
# As variáveis dos comandos SQL são expandidas dentro do container.
# shellcheck disable=SC2016
set -Eeuo pipefail
source "$(dirname -- "${BASH_SOURCE[0]}")/comum.sh"

arquivo="${1:?Informe o arquivo .dump a restaurar}"
[[ -f "$arquivo" ]] || { printf 'Backup não encontrado.\n' >&2; exit 1; }
banco="restore_euconomia_$(date -u +%Y%m%d%H%M%S)_$$"

compose exec -T postgres sh -ec \
  'createdb --username "$POSTGRES_USER" --owner "$EUCONOMIA_DB_USER" "$1"' sh "$banco"
compose exec -T postgres sh -ec \
  'pg_restore --username "$EUCONOMIA_DB_USER" --dbname "$1" --no-owner --exit-on-error --single-transaction' sh "$banco" < "$arquivo"
printf 'Backup restaurado no banco %s. Confira os dados antes de alterar POSTGRES_DB no .env e executar o deploy.\n' "$banco"
