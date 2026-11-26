param([string]$Diretorio = (Join-Path $PSScriptRoot 'backups'))

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$nome = 'euconomia-{0}-{1}.dump' -f (Get-Date -Format 'yyyyMMdd-HHmmss'), [guid]::NewGuid().ToString('N')
$remoto = "/tmp/$nome"
$null = New-Item -ItemType Directory -Path $Diretorio -Force
$destino = Join-Path (Resolve-Path -LiteralPath $Diretorio).Path $nome
$temporario = "$destino.part"

Push-Location $PSScriptRoot
try {
    docker compose exec -T postgres sh -ec 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f "$1"; pg_restore -l "$1" >/dev/null' sh $remoto
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao gerar o backup.' }
    docker compose cp "postgres:$remoto" $temporario
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao copiar o backup.' }
    Move-Item -LiteralPath $temporario -Destination $destino
    Write-Output $destino
}
finally {
    docker compose exec -T postgres rm -f -- $remoto | Out-Null
    if (Test-Path -LiteralPath $temporario) { Remove-Item -LiteralPath $temporario }
    Pop-Location
}
