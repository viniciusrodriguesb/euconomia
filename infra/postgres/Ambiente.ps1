param([string]$Banco)

$arquivo = Join-Path $PSScriptRoot '.env'
if (-not (Test-Path -LiteralPath $arquivo)) {
    throw 'Crie infra/postgres/.env a partir de .env.example e defina a senha.'
}

$config = @{}
foreach ($linha in Get-Content -LiteralPath $arquivo) {
    if ($linha -match '^\s*(POSTGRES_(?:DB|USER|PASSWORD|PORT))=(.*?)\s*$') {
        $config[$Matches[1]] = $Matches[2]
    }
}
foreach ($campo in @('POSTGRES_DB', 'POSTGRES_USER', 'POSTGRES_PASSWORD')) {
    if (-not $config[$campo]) { throw "Defina $campo no .env." }
}

$connection = [System.Data.Common.DbConnectionStringBuilder]::new()
$connection['Host'] = '127.0.0.1'
$connection['Port'] = if ($config.POSTGRES_PORT) { $config.POSTGRES_PORT } else { 5433 }
$connection['Database'] = if ($Banco) { $Banco } else { $config.POSTGRES_DB }
$connection['Username'] = $config.POSTGRES_USER
$connection['Password'] = $config.POSTGRES_PASSWORD
$env:ConnectionStrings__Financeiro = $connection.ConnectionString
