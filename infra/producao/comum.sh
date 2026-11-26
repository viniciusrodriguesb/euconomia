#!/usr/bin/env bash

EUCONOMIA_DIR="${EUCONOMIA_DIR:-/opt/euconomia}"
RELEASE_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"

if [[ ! -f "$EUCONOMIA_DIR/.env" ]]; then
  printf 'Crie %s/.env com base em .env.example antes de continuar.\n' "$EUCONOMIA_DIR" >&2
  exit 1
fi

COMPOSE_ARGS=(
  --project-name euconomia-producao
  --project-directory "$RELEASE_DIR"
  --env-file "$EUCONOMIA_DIR/.env"
)
if [[ -f "$EUCONOMIA_DIR/.imagem.env" ]]; then
  COMPOSE_ARGS+=(--env-file "$EUCONOMIA_DIR/.imagem.env")
fi
COMPOSE_ARGS+=(-f "$RELEASE_DIR/compose.yml")

compose() {
  docker compose "${COMPOSE_ARGS[@]}" "$@"
}
