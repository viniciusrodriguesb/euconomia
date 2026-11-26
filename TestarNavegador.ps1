param([switch]$UsarDependenciasInstaladas)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$banco = 'e2e_euconomia_' + [guid]::NewGuid().ToString('N')
$postgres = Join-Path $PSScriptRoot 'infra/postgres'
$web = Join-Path $PSScriptRoot 'src/Web'
$publicacao = Join-Path $PSScriptRoot '.publicacao/e2e'
$npm = Join-Path (Split-Path (Get-Command npm.cmd).Source) 'node_modules/npm/bin/npm-cli.js'
$variaveis = @('ConnectionStrings__Financeiro', 'DOTNET_CLI_HOME', 'ASPNETCORE_ENVIRONMENT', 'ASPNETCORE_URLS', 'EUCONOMIA_E2E_DLL', 'EUCONOMIA_URL')
$anterior = @{}
foreach ($nome in $variaveis) { $anterior[$nome] = [Environment]::GetEnvironmentVariable($nome) }
$criado = $false

Push-Location $PSScriptRoot
try {
    $env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.dotnet-local'
    Push-Location $postgres
    try {
        docker compose exec -T postgres sh -ec 'createdb -U "$POSTGRES_USER" -T template0 "$1"' sh $banco
        $criado = $true
    } finally { Pop-Location }
    & (Join-Path $postgres 'Ambiente.ps1') -Banco $banco
    dotnet tool restore
    dotnet ef database update --project src/Infraestrutura
    Push-Location $web
    try {
        if (-not $UsarDependenciasInstaladas) {
            npm exec --yes --package=node@22.23.5 -- node $npm ci
        }
        npm exec --yes --package=node@22.23.5 -- node node_modules/@angular/cli/bin/ng.js build
    } finally { Pop-Location }
    dotnet publish src/Api -c Release -o $publicacao
    $env:EUCONOMIA_E2E_DLL = Join-Path $publicacao 'Api.dll'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = 'http://127.0.0.1:5278'
    $env:EUCONOMIA_URL = $env:ASPNETCORE_URLS
    Push-Location $web
    try { npm exec --yes --package=node@22.23.5 -- node node_modules/@playwright/test/cli.js test }
    finally { Pop-Location }
}
finally {
    try {
        if ($criado) {
            Push-Location $postgres
            try { docker compose exec -T postgres sh -ec 'dropdb -U "$POSTGRES_USER" --force "$1"' sh $banco }
            finally { Pop-Location }
        }
    }
    finally {
        foreach ($nome in $variaveis) { [Environment]::SetEnvironmentVariable($nome, $anterior[$nome]) }
        Pop-Location
    }
}
