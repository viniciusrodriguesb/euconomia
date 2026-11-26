param(
    [Parameter(Mandatory)][string]$Arquivo,
    [Parameter(Mandatory)][ValidatePattern('^restore_[a-z0-9_]{1,55}$')][string]$Banco
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$origem = (Resolve-Path -LiteralPath $Arquivo).Path
$remoto = '/tmp/restore-{0}.dump' -f [guid]::NewGuid().ToString('N')

Push-Location $PSScriptRoot
try {
    docker compose cp $origem "postgres:$remoto"
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao enviar o arquivo ao contêiner.' }
    docker compose exec -T postgres sh -ec 'test "$1" != "$POSTGRES_DB"; pg_restore -l "$2" >/dev/null; createdb -U "$POSTGRES_USER" -T template0 "$1"; if ! pg_restore -U "$POSTGRES_USER" --exit-on-error --single-transaction -d "$1" "$2"; then dropdb -U "$POSTGRES_USER" "$1"; exit 1; fi' sh $Banco $remoto
    if ($LASTEXITCODE -ne 0) { throw 'Falha na restauração. Confira o arquivo e use um nome de banco ainda disponível.' }
    Write-Output $Banco
}
finally {
    docker compose exec -T postgres rm -f -- $remoto | Out-Null
    Pop-Location
}
