# Euconomia

A solução é `Euconomia.slnx`, e o pacote e o diretório de saída do Angular usam `euconomia`. A migration `MarcaEuconomia` preserva códigos de recuperação emitidos com a marca anterior. A troca dos nomes dos cookies exige entrar novamente.

O ambiente PostgreSQL já existente mantém seus identificadores de banco, usuário e armazenamento. O `.env` local fixa `COMPOSE_PROJECT_NAME` para reutilizar o container existente. Instalações novas usam `euconomia` conforme `.env.example` e `docker-compose.yml`.

Controle financeiro pessoal com ASP.NET Core, Angular e PostgreSQL. Em desenvolvimento, o PostgreSQL roda no Docker Desktop e a API e o Angular rodam no Windows. A publicação serve a API e a SPA no mesmo processo e endereço.

O [guia de implantação na VPS Hostinger](infra/producao/README.md) descreve o GitHub Actions, o build Docker de produção, o HTTPS, as credenciais e os procedimentos de backup e restauração. A implantação por SSH fica desabilitada até configurar a VPS e habilitar `VPS_DEPLOY_ENABLED`.

## 0 · Descoberta

- SDK **.NET 10 LTS 10.0.404**, selecionado em `global.json`. Valide com `dotnet --list-sdks` e `dotnet --version`.
- **Angular CLI 19.2.15**, Angular 19.2.x e **Node 22**. O fluxo foi compilado com Node 22.23.5. O Node 24 instalado globalmente está fora da matriz do Angular 19.2. A série 19 já encerrou o suporte oficial. [Compatibilidade do Angular](https://angular.dev/reference/versions)
- A auditoria de dependências em 16/11/2026 registrou 6 avisos no conjunto de produção, 4 com severidade alta. Reproduza com `npm audit --omit=dev` em `src/Web`.
- **Docker Desktop com Compose** e imagem local **postgres:16.15-alpine**. Confirme com `docker compose version`, `docker images` e `docker ps`.
- A regra confirmada para **Retirada de saldo** reduz o caixa e tem linha própria nos relatórios. As entradas, despesas categorizadas e o patrimônio investido mantêm os valores anteriores à retirada.
- Foi usada a porta **5433**, vinculada a `127.0.0.1`. A pasta `D:\Trabalho\postgres-local` já hospedava um Compose ativo de outro projeto na 5432. O Compose desta aplicação fica em `infra/postgres`, com volume próprio.

Os comandos a seguir usam PowerShell 7. Comece na raiz do projeto.

## 1 · PostgreSQL

```powershell
Set-Location infra/postgres
if (-not (Test-Path .env)) {
    $senha = [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    (Get-Content -Raw .env.example).Replace('POSTGRES_PASSWORD=', "POSTGRES_PASSWORD=$senha") |
        Set-Content -LiteralPath .env -Encoding utf8NoBOM
}
docker compose up -d --wait
docker compose ps
docker compose exec postgres sh -ec 'PGPASSWORD="$POSTGRES_PASSWORD" psql -h 127.0.0.1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "show data_checksums; show fsync; show synchronous_commit; show full_page_writes; show timezone;"'
Set-Location ../..
```

O volume nomeado preserva o banco entre reinícios. O cluster é inicializado com checksums e opera com `fsync`, `synchronous_commit` e `full_page_writes` ligados. Ao alterar a porta no `.env`, `Ambiente.ps1` carrega a nova porta para o .NET. Use valores simples no `.env` local, incluindo a senha hexadecimal gerada acima.

### Backup e restauração

```powershell
$dump = ./infra/postgres/Backup.ps1
./infra/postgres/Restore.ps1 -Arquivo $dump -Banco restore_conferencia
Set-Location infra/postgres
docker compose exec postgres sh -ec 'psql -U "$POSTGRES_USER" -d restore_conferencia -c "\dt"'
Set-Location ../..
```

`Backup.ps1` cria um `pg_dump -Fc` com data, hora e identificador no nome. Use `-Diretorio` para gravá-lo no destino de backup. Para agendar, configure no Agendador de Tarefas o programa `pwsh.exe`, os argumentos `-NoProfile -File "D:\Trabalho\Repositorios\Euconomia\infra\postgres\Backup.ps1"` e a conta que pode acessar o Docker Desktop. Confirme a recuperação de uma amostra periodicamente.

`Restore.ps1` exige um banco novo com nome começando por `restore_`. Ele restaura em uma transação e verifica o código de saída do PostgreSQL. Para recuperar o banco de uso, confira a cópia restaurada primeiro, pare a API e aponte `ConnectionStrings__Financeiro` para o banco restaurado. Mantenha os arquivos de backup em armazenamento separado e com acesso restrito.

## 2 · Modelo e migrations

```powershell
./infra/postgres/Ambiente.ps1
dotnet tool restore
dotnet ef migrations list --project src/Infraestrutura
dotnet ef database update --project src/Infraestrutura
dotnet ef migrations has-pending-model-changes --project src/Infraestrutura
```

- As entidades usam UUID v7 gerado no .NET para manter a ordenação aproximada por criação. Chaves e FKs dos dados financeiros incluem `usuario_id`, permitindo bloquear referências entre contas de usuários distintos diretamente no PostgreSQL.
- `lancamento`, `compra_parcelada` e `auditoria` têm proteção de imutabilidade. A compra parcelada só confirma a transação com todas as parcelas e o total exato. Categorias padrão e configuração são criadas na transação de registro do usuário.
- Dinheiro é `decimal` no servidor, `numeric(18,2)` no banco, texto decimal na resposta JSON e centavos inteiros na interface. Datas financeiras são `DateOnly` e `date`. Competência é armazenada no primeiro dia do mês.
- Consultas financeiras aplicam escopo explícito de usuário e FKs compostas. Para operação com um papel de aplicação separado do proprietário das tabelas, RLS pode ser adicionada com `SET LOCAL` do usuário dentro da transação, políticas testadas nos relatórios e um papel separado para migrations e restauração. A configuração local desta entrega usa o papel de desenvolvimento do Docker.
- Para volume alto, meça crescimento, latência e tempo de manutenção do ledger. O particionamento proposto é **hash por `usuario_id`** quando manutenção de índices ou volume de I/O justificar a mudança. As chaves únicas atuais incluem a coluna de partição. Relatórios agregados mensais podem ser introduzidos como cache recalculável após medir os planos de consulta.

## 3 · API

```powershell
./infra/postgres/Ambiente.ps1
dotnet run --project src/Api --launch-profile http
```

A API fica em `http://localhost:5202/api`. A SPA usa sessão do Identity por cookie `HttpOnly` e token antifalsificação em alterações. Em produção, o cookie exige HTTPS. Registre a conta pela tela inicial para receber as categorias padrão.

No Visual Studio, abra `Euconomia.slnx`, defina `src/Api` como projeto de inicialização e selecione o perfil `http` antes de pressionar F5. O arquivo local `src/Api/appsettings.Development.json` contém a conexão `ConnectionStrings:Financeiro` com o PostgreSQL de `infra/postgres/.env`. Ele está ignorado pelo Git por conter credenciais locais. Se alterar o banco ou a senha no `.env`, atualize essa conexão também. Uma variável `ConnectionStrings__Financeiro` definida no ambiente tem precedência sobre o arquivo.

Mantenha o PostgreSQL iniciado com `docker compose up -d --wait` na pasta `infra/postgres`. Os perfis da API iniciam o backend sem abrir o navegador. Para acessar a interface durante o desenvolvimento, inicie o Angular com `npm start` em `src/Web` e abra `http://127.0.0.1:4200`.

- Compras no crédito entram integralmente na competência da compra. O calendário das parcelas distribui os valores pelas faturas. O caixa reflete o pagamento da fatura na data informada.
- O dia do fechamento inclui compras feitas naquele dia. Dias 29 a 31 que extrapolam o mês usam o último dia disponível. Faturas já criadas conservam suas datas quando o cartão é editado.
- Para corrigir uma compra de fatura paga, primeiro estorne o pagamento. O estorno reabre o valor a pagar. A correção registra o inverso e o substituto na mesma transação.
- Escritas do mesmo usuário são serializadas enquanto a transação está aberta. A chave de idempotência devolve a resposta original para reenvios equivalentes. Quem editar uma versão desatualizada recebe HTTP 409.
- O agendamento recorrente é opcional, por usuário. Na virada do mês, o serviço interno confirma ocorrências pendentes dos meses encerrados desde que a opção foi ativada. O cursor mensal e a idempotência permitem retomar após uma parada. Para agendar um boleto avulso, use uma regra de custo fixo com início e fim na mesma competência.
- O limite padrão de comprometimento futuro é **80%** da média das receitas dos seis meses completos anteriores. A taxa de poupança usa as entradas menos as saídas, com os aportes tratados como poupança. O percentual livre desconta também os aportes.

### Cadastro e recuperação da conta

O cadastro exige e-mail e senha com suas confirmações no frontend e no servidor. Após entrar, a pessoa recebe um código aleatório de recuperação de 256 bits, exibido uma única vez e armazenado como hash SHA-256 nos tokens do Identity. A página **Segurança**, acessível pelo cabeçalho, permite gerar um código com a senha atual, inclusive para contas existentes. O novo código invalida o anterior.

Em **Esqueci minha senha**, informe e-mail, código e a nova senha com confirmação. A redefinição consome o código, remove o bloqueio de login e invalida as sessões anteriores. O consumo e a troca de senha ocorrem na mesma transação, com bloqueio da linha do usuário para impedir reuso simultâneo. Após entrar novamente, gere outro código. A perda simultânea da senha e do código impede a recuperação por esse fluxo. A confirmação por digitação confere os campos, sem verificar a posse da caixa de e-mail.

As operações de login e cadastro aceitam até 30 tentativas por IP em 15 minutos. As operações de geração e uso do código compartilham o limite de 10 tentativas por IP em 15 minutos. Cinco senhas incorretas bloqueiam o login da conta por 15 minutos. As respostas de recuperação são genéricas para dados incorretos, e todas as escritas exigem token antifalsificação. Os limites em memória valem por instância. Ao publicar atrás de proxy reverso, configure os cabeçalhos encaminhados apenas para proxies conhecidos para que a aplicação identifique os IPs dos clientes.

## 4 · Interface Angular

Em outro terminal, com Node 22 no `PATH`:

```powershell
Set-Location src/Web
node --version
npm ci
npm start
```

Abra `http://127.0.0.1:4200`. O proxy do Angular encaminha as rotas `/api` para o ASP.NET Core. A aplicação usa componentes standalone, signals, OnPush, formulários tipados e rotas lazy com `@if` e `@for`. Nenhuma API experimental do Angular é usada.

Com o Node 24 ainda global, estes comandos executam o npm e o Angular explicitamente no Node 22:

```powershell
Set-Location src/Web
$npm = Join-Path (Split-Path (Get-Command npm.cmd).Source) 'node_modules/npm/bin/npm-cli.js'
npm exec --yes --package=node@22.23.5 -- node $npm ci
npm exec --yes --package=node@22.23.5 -- node node_modules/@angular/cli/bin/ng.js serve --proxy-config proxy.conf.json --host 127.0.0.1
```

### Publicação em um processo

Com Node 22 no `PATH` e a partir da raiz:

```powershell
npm --prefix src/Web ci
npm --prefix src/Web run build
dotnet publish src/Api -c Release -o .publicacao/app
```

O diretório `.publicacao/app` contém a API e os arquivos da SPA em `wwwroot`. Na máquina de destino, aplique as migrations com as credenciais da operação de banco, configure a conexão de execução, mantenha as chaves de Data Protection persistentes e execute `dotnet Api.dll` com HTTPS. Mantenha o PostgreSQL em armazenamento durável.

Para abrir o pacote localmente após aplicar as migrations:

```powershell
./infra/postgres/Ambiente.ps1
Set-Location .publicacao/app
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet Api.dll --urls http://127.0.0.1:5202
```

Abra `http://127.0.0.1:5202`. A API e a SPA passam pelo mesmo endereço.

## 5 · Conferência e testes

```powershell
./infra/postgres/Ambiente.ps1
dotnet test
./TestarNavegador.ps1
```

`dotnet test` cria bancos descartáveis no PostgreSQL local. Os testes da API cobrem os 12 critérios financeiros, a proteção entre usuários, a rejeição de uma compra incompleta, a detecção de divergência e a repetição segura da confirmação automática. `TestarNavegador.ps1` usa o Edge no Windows, publica a interface junto à API e valida o fluxo com um banco próprio. O teste inclui o layout em uma tela de 390 px e a checagem automatizada de acessibilidade do dashboard com regras WCAG A e AA. O script descarta esse banco ao terminar.

Na interface, **Visão geral → Conferir registros** chama `GET /api/conferencia`. A rotina compara o cálculo do ledger com o saldo exibido, o total da view de faturas e os pagamentos ativos. Também verifica a contagem, a sequência, o valor e o vínculo de cada parcela. A leitura usa um snapshot transacional para evitar divergências falsas durante gravações.
