# Implantação do Euconomia na Hostinger

Preparado para uma VPS Ubuntu 24.04 LTS x64 com Docker Engine e Docker Compose. O GitHub Actions compila e testa o projeto, publica a imagem no GitHub Container Registry e, quando habilitado, implanta por SSH. A VPS executa PostgreSQL, API com Angular e Caddy para HTTPS.

O deploy permanece desabilitado até a variável de repositório `VPS_DEPLOY_ENABLED` receber `true`. Configure a VPS e o domínio antes de habilitar.

## 1. Preparar a VPS

No hPanel, crie a VPS e aponte o registro DNS A do domínio para o IPv4 dela. Caso publique um registro AAAA, ele também deve apontar para a VPS. Libere as portas TCP 80 e 443 e a porta SSH escolhida no firewall da Hostinger. UDP 443 habilita HTTP/3. As portas do PostgreSQL e da API ficam internas ao Docker.

Conecte por SSH e instale Docker Engine e o plugin Compose seguindo a [documentação oficial para Ubuntu](https://docs.docker.com/engine/install/ubuntu/). Confira a instalação e os comandos auxiliares.

```bash
sudo apt-get update
sudo apt-get install -y curl openssl util-linux
docker version
docker compose version
```

Crie um usuário dedicado à implantação e a pasta de trabalho.

```bash
sudo adduser --disabled-password --gecos "" euconomia
sudo usermod -aG docker euconomia
sudo install -d -m 0750 -o euconomia -g euconomia /opt/euconomia
sudo install -d -m 0750 -o euconomia -g euconomia /opt/euconomia/releases /opt/euconomia/backups
sudo install -d -m 0700 -o euconomia -g euconomia /home/euconomia/.ssh
```

O grupo `docker` concede controle administrativo sobre o host. Use essa conta exclusivamente para implantação. Abra uma nova sessão SSH com ela para aplicar a participação no grupo.

## 2. Configurar as credenciais da VPS

Entre como `euconomia`. Crie `/opt/euconomia/.env` com base no arquivo [.env.example](.env.example), preenchendo domínio e e-mail. Gere duas senhas independentes com `openssl rand -hex 32` e use-as nos respectivos campos.

```dotenv
EUCONOMIA_DOMAIN=euconomia.seudominio.com.br
ACME_EMAIL=seu-email@example.com
POSTGRES_DB=euconomia
EUCONOMIA_DB_USER=euconomia
POSTGRES_PASSWORD=senha-administrativa-gerada
EUCONOMIA_DB_PASSWORD=outra-senha-gerada
```

```bash
chmod 600 /opt/euconomia/.env
```

O PostgreSQL inicializa o usuário da aplicação sem privilégios de superusuário. As migrations usam esse proprietário do banco. O usuário administrativo fica restrito ao container do PostgreSQL.

O arquivo `.env` fica somente na VPS. Ele é ignorado pelo Git e pelo contexto de build. Alterar as senhas nesse arquivo após inicializar o volume exige alterar as senhas dos papéis no PostgreSQL também. Trocar o texto do arquivo não altera as credenciais do banco existente.

Se já houver uma rede Docker em `172.30.18.0/24`, escolha outra sub-rede privada e informe também `EUCONOMIA_PROXY_SUBNET`, `EUCONOMIA_PROXY_IP` e `EUCONOMIA_APP_IP` no `.env`. Os dois IPs devem ser diferentes e pertencer à sub-rede escolhida. A API confia nos cabeçalhos encaminhados apenas pelo endereço do Caddy e pelo loopback.

## 3. Configurar a chave SSH

Na sua máquina, gere uma chave exclusiva para o GitHub Actions.

```powershell
ssh-keygen -t ed25519 -f "$HOME/.ssh/euconomia_deploy" -C "github-actions-euconomia"
```

O fluxo usa uma chave de implantação sem passphrase, guardada como secret do GitHub. Instale o conteúdo do arquivo público `euconomia_deploy.pub` em `/home/euconomia/.ssh/authorized_keys` na VPS. A chave privada será usada somente no secret.

Na VPS, ajuste as permissões.

```bash
sudo chown euconomia:euconomia /home/euconomia/.ssh/authorized_keys
sudo chmod 600 /home/euconomia/.ssh/authorized_keys
```

Para obter a chave pública do servidor, use o console da Hostinger.

```bash
sudo cat /etc/ssh/ssh_host_ed25519_key.pub
sudo ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub
```

No secret de hosts conhecidos, use a linha `IP_DA_VPS ssh-ed25519 CHAVE_PUBLICA_DO_SERVIDOR`. Para uma porta SSH diferente de 22, use `[IP_DA_VPS]:PORTA ssh-ed25519 CHAVE_PUBLICA_DO_SERVIDOR`. Mantenha o host igual ao cadastrado em `VPS_HOST`. O workflow exige a correspondência dessa chave.

## 4. Habilitar a leitura da imagem privada

As imagens publicadas em `ghcr.io/usuario/repositorio` podem permanecer privadas. Crie no GitHub um token clássico dedicado com permissão `read:packages` e acesso ao pacote. Na VPS, como `euconomia`, autentique sem gravar o token no histórico.

```bash
read -r -p "Usuário GitHub: " ghcr_usuario
read -r -s -p "Token de leitura do GHCR: " ghcr_token
printf '\n'
printf '%s' "$ghcr_token" | docker login ghcr.io --username "$ghcr_usuario" --password-stdin
unset ghcr_token
```

Uma imagem pública dispensa esse login. O workflow usa o `GITHUB_TOKEN` do próprio repositório para publicar a imagem, sem precisar de um token pessoal de escrita.

## 5. Configurar o GitHub

Em **Settings → Environments**, crie o ambiente `producao`. Adicione os secrets abaixo nesse ambiente.

| Secret | Conteúdo |
|---|---|
| `VPS_HOST` | IP ou hostname da VPS |
| `VPS_USER` | `euconomia` |
| `VPS_SSH_PRIVATE_KEY` | Conteúdo completo da chave privada de implantação |
| `VPS_SSH_KNOWN_HOSTS` | Linha de host conhecido obtida no console da VPS |

Em **Settings → Secrets and variables → Actions → Variables**, configure as variáveis de repositório.

| Variável | Valor |
|---|---|
| `VPS_DEPLOY_ENABLED` | `false` durante a preparação, depois `true` |
| `VPS_PORT` | Porta SSH, com padrão `22` |

Envie o projeto para a branch `main`. O workflow [.github/workflows/euconomia.yml](../../.github/workflows/euconomia.yml) executa tipos, lint, testes unitários com cobertura mínima de 90% por arquivo executável, testes de integração, build do Angular e testes de navegador. Os bancos do CI são descartáveis.

Em pull requests, a imagem é compilada para validar o Dockerfile. Na `main`, ela também é publicada com uma tag `sha-COMMIT`. O deploy recebe o digest imutável da imagem que acabou de ser construída. As actions estão fixadas por SHA.

Com a VPS pronta e o domínio resolvendo para ela, altere `VPS_DEPLOY_ENABLED` para `true` e execute **Actions → Euconomia - CI e deploy → Run workflow** na `main`. Novos pushes nessa branch repetem o fluxo.

## 6. Como a atualização funciona

O workflow envia os arquivos de implantação para `/opt/euconomia/releases/COMMIT` e executa `deploy.sh`. A configuração e as credenciais ficam em `/opt/euconomia/.env`.

O script bloqueia deploys simultâneos, baixa as imagens, valida o Caddy, verifica o PostgreSQL e cria um backup. Em seguida, para brevemente a API, aplica as migrations e sobe a nova aplicação. O healthcheck testa a conexão com o banco. O Caddy fornece o certificado HTTPS automaticamente, e o deploy verifica a página pública por HTTPS antes de registrar a imagem como atual.

As chaves do ASP.NET Core Data Protection persistem em um volume próprio. Os certificados HTTPS e os dados do PostgreSQL também persistem. A aplicação executa como usuário sem privilégios de root e com sistema de arquivos somente leitura, exceto o diretório de chaves e o temporário.

Após sucesso, `/opt/euconomia/current` aponta para a release atual. A referência da imagem fica em `.imagem.env` e a anterior em `.imagem-anterior.env`. Falhas em migrations ou no healthcheck interrompem a atualização e tentam iniciar a imagem anterior quando ela existe. Migrations já concluídas permanecem aplicadas. Confira a compatibilidade do schema antes de fazer rollback de uma alteração de banco.

## 7. Operação e backups

Como `euconomia`, use os arquivos da release atual.

```bash
cd /opt/euconomia/current
source ./comum.sh
compose ps
compose logs --tail=100 app caddy
bash ./backup.sh
```

O backup é validado com `pg_restore --list` e gravado em `/opt/euconomia/backups` com permissões privadas. O deploy cria um backup antes das migrations. Para um backup diário às 03h no fuso horário configurado no host, adicione ao crontab do usuário `euconomia`.

```cron
0 3 * * * /bin/bash /opt/euconomia/current/backup.sh >> /opt/euconomia/backup.log 2>&1
```

Copie os backups periodicamente para um armazenamento externo à VPS. Defina uma retenção adequada ao espaço disponível. Inclua os volumes de chaves e certificados no plano de backup da VPS. O comando `docker compose down -v` apaga os volumes e não faz parte do procedimento de atualização.

Para testar a restauração sem substituir o banco em uso, execute o script com um dump.

```bash
bash /opt/euconomia/current/restaurar.sh /opt/euconomia/backups/arquivo.dump
```

O script cria um banco `restore_euconomia_...` e restaura em uma transação. Confira os dados restaurados. Para passar a usá-lo, altere `POSTGRES_DB` no `.env` e execute o deploy com a imagem desejada. O banco anterior continua disponível.

Para republicar a imagem anterior, leia a referência registrada e execute o deploy.

```bash
imagem="$(sed -n 's/^EUCONOMIA_IMAGE=//p' /opt/euconomia/.imagem-anterior.env)"
bash /opt/euconomia/current/deploy.sh "$imagem"
```

Para migrar os dados locais para a VPS, gere o dump com `infra/postgres/Backup.ps1`, copie-o por SCP para uma pasta privada e use `restaurar.sh`. Valide a restauração antes de alterar o banco configurado. Esse processo inclui os usuários e os dados financeiros do dump.

## Referências

- [Docker Engine no Ubuntu](https://docs.docker.com/engine/install/ubuntu/)
- [Publicação de imagens pelo GitHub Actions](https://docs.github.com/en/actions/tutorials/publish-packages/publish-docker-images)
- [HTTPS automático do Caddy](https://caddyserver.com/docs/automatic-https)
- [Proxy reverso no ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
