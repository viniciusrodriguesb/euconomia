#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

export EUCONOMIA_IMAGE="${1:?Informe a imagem da aplicação com tag ou digest}"
source "$(dirname -- "${BASH_SOURCE[0]}")/comum.sh"

exec 9>"$EUCONOMIA_DIR/.deploy.lock"
flock -w 900 9
compose config --quiet
compose pull app migracoes postgres caddy
compose run --rm --no-deps validar-caddy caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile
compose up -d --wait --wait-timeout 90 postgres
bash "$RELEASE_DIR/backup.sh"

anterior=""
if [[ -f "$EUCONOMIA_DIR/.imagem.env" ]]; then
  anterior="$(sed -n 's/^EUCONOMIA_IMAGE=//p' "$EUCONOMIA_DIR/.imagem.env")"
fi

recuperar_app() {
  if [[ -n "$anterior" ]]; then
    printf 'Tentando iniciar a imagem anterior da aplicação. Confira a compatibilidade das migrations.\n' >&2
    EUCONOMIA_IMAGE="$anterior" compose up -d --no-deps --wait --wait-timeout 120 app || true
  fi
}

compose stop app
if ! compose run --rm --no-deps migracoes; then
  recuperar_app
  printf 'Migração falhou. O deploy foi interrompido. Confira os logs e o backup.\n' >&2
  exit 1
fi
if ! compose up -d --no-deps --wait --wait-timeout 120 app; then
  recuperar_app
  printf 'A nova aplicação falhou no healthcheck.\n' >&2
  exit 1
fi
compose up -d --no-deps caddy
dominio="$(compose exec -T caddy printenv EUCONOMIA_DOMAIN)"
curl --fail --silent --show-error --retry 12 --retry-all-errors --retry-delay 5 "https://$dominio/" > /dev/null

if [[ -n "$anterior" ]]; then
  printf 'EUCONOMIA_IMAGE=%s\n' "$anterior" > "$EUCONOMIA_DIR/.imagem-anterior.env"
fi
printf 'EUCONOMIA_IMAGE=%s\n' "$EUCONOMIA_IMAGE" > "$EUCONOMIA_DIR/.imagem.env.tmp"
mv -f -- "$EUCONOMIA_DIR/.imagem.env.tmp" "$EUCONOMIA_DIR/.imagem.env"
ln -s -- "$RELEASE_DIR" "$EUCONOMIA_DIR/.current-$$"
mv -Tf -- "$EUCONOMIA_DIR/.current-$$" "$EUCONOMIA_DIR/current"
printf 'Deploy concluído. Imagem %s\n' "$EUCONOMIA_IMAGE"
