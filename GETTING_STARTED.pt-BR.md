# Getting Started — Kyvo

[English](./GETTING_STARTED.md) | [Português](./GETTING_STARTED.pt-BR.md)

> **Pronúncia:** *Kyvo* pronuncia-se como **"Key"vo** — parecido com a palavra inglesa *key* + *vo*.

Guia para rodar o Kyvo em **desenvolvimento** (Docker Compose + `.env` neste repositório) ou **produção** (imagens Docker publicadas).

### Escolha o caminho

| Caminho | Público | Seções |
|---------|---------|--------|
| **Desenvolvimento** | Você clonou o repositório e roda API e SPA com Docker Compose + `.env` | **1–6** abaixo |
| **Produção** | Você implanta imagens publicadas com Docker Compose (sem build deste repo) | **[§ 7 — Deploy em produção](#7-deploy-em-produção-docker-compose)** |

> **Mantenedores** (build e push de imagens): veja [docs/DOCKER_PUBLISH.pt-BR.md](./docs/DOCKER_PUBLISH.pt-BR.md), não este guia.

---

## Desenvolvimento (seções 1–6)

---

## 1. Pré-requisitos

Instale antes de continuar:

| Ferramenta | Como instalar | Versão mínima | Finalidade |
|------------|---------------|---------------|------------|
| Docker Engine + Compose v2 | [docker.com](https://docs.docker.com/get-docker/) | Atual | Rodar containers da API e do SPA admin |
| PostgreSQL | [postgresql.org](https://www.postgresql.org/download/) | 14 | Banco no **host** (fora do compose do Kyvo) |
| Redis | [redis.io](https://redis.io/downloads/) | Opcional | Cache no host; API usa in-memory se vazio |
| .NET SDK | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0 | Rodar migrations `dotnet ef` no host |
| dotnet-ef (CLI) | `dotnet tool install --global dotnet-ef` | 8.x | Aplicar migrations EF |
| openssl | macOS/Linux incluso; Windows: Git for Windows ou `winget install ShiningLight.OpenSSL` | Qualquer | Gerar chave RSA de assinatura OIDC |

Clone o repositório:

```bash
git clone https://github.com/mrffilipe/kyvo.git
cd kyvo
```

---

## 2. Configurar o banco de dados

Crie um banco PostgreSQL na sua máquina (ou em outro servidor que você gerencie). O container da API alcança o banco via **`host.docker.internal`** (veja [backend/.env.example](./backend/.env.example)).

```sql
CREATE DATABASE kyvo_db;
```

Ou via linha de comando:

```bash
createdb kyvo_db
```

PostgreSQL e Redis **não** estão em [backend/docker-compose.yml](./backend/docker-compose.yml). Rode-os no host (ou em outro lugar) e aponte `Database__ConnectionString` / `Redis__ConnectionString` em `backend/.env` para `host.docker.internal`.

| Quem conecta | Host na connection string | Motivo |
|--------------|---------------------------|--------|
| Container da API (`backend/.env`) | `host.docker.internal` | DNS do Docker para serviços no host |
| `dotnet ef` no host | `localhost` | CLI roda fora do container |

---

## 3. Configurar o backend

O desenvolvimento usa [backend/docker-compose.yml](./backend/docker-compose.yml) e [backend/.env.example](./backend/.env.example). A configuração é dividida entre **`.env`** (container da API) e **`appsettings.Development.json`** (`dotnet ef` no host):

| Arquivo | Usado por | Host em `Database` |
|---------|-----------|-------------------|
| `backend/.env` | `docker compose` (container da API) | `host.docker.internal` |
| `Kyvo.API/appsettings.Development.json` | `dotnet ef` (host) | `localhost` |

Mantenha credenciais e demais valores alinhados nos dois arquivos (veja comentários em `.env.example`).

### 3.1 Preparar o arquivo `.env`

```bash
cd backend
cp .env.example .env
```

Edite `.env` com credenciais do PostgreSQL/Redis, admin de bootstrap e opções do compose (`API_PORT`). O template já usa `host.docker.internal` para banco e Redis.

### 3.2 Chave de assinatura OIDC (RSA)

O Kyvo assina tokens OIDC com **RS256** (RSA + SHA-256). Configure **exatamente uma** fonte de chave. Nunca commite o PEM (`backend/keys/*.pem` está no `.gitignore`).

| Cenário | Variável | Como fornecer a chave |
|---------|----------|------------------------|
| **Desenvolvimento** (compose) | `Jwt__SigningKeyPath=keys/oidc-signing.pem` | Gerar PEM em `backend/keys/oidc-signing.pem` **antes** do primeiro `docker compose up`; compose monta `./keys` |
| **Produção** (§7) | `Jwt__SigningKeyPemBase64` | Gerar PEM fora do repo → codificar Base64 → colar no `.env` de deploy; **não** montar arquivo |
| Evitar | Múltiplas fontes | Defina apenas **uma** entre Path / Pem / PemBase64 |

**Gerar o PEM** (chave privada RSA 2048 bits) **antes** de subir o Docker Compose. Se o arquivo não existir, o Docker Desktop pode criar `keys/oidc-signing.pem` como **pasta** e o container falha — apague essa pasta e gere um arquivo PEM de verdade.

```bash
cd backend
mkdir -p keys
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out keys/oidc-signing.pem
```

**Windows** (OpenSSL no PATH):

```powershell
cd backend
New-Item -ItemType Directory -Force -Path keys | Out-Null
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out keys/oidc-signing.pem
```

**Windows** (sem OpenSSL — Docker):

```powershell
cd backend
New-Item -ItemType Directory -Force -Path keys | Out-Null
docker run --rm -v "${PWD}/keys:/keys" alpine/openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out /keys/oidc-signing.pem
```

**Windows** (sem OpenSSL — .NET 5+ / PowerShell 7+):

```powershell
cd backend
New-Item -ItemType Directory -Force -Path keys | Out-Null
$rsa = [System.Security.Cryptography.RSA]::Create(2048)
[System.IO.File]::WriteAllText("$PWD\keys\oidc-signing.pem", $rsa.ExportPkcs8PrivateKeyPem())
```

**Ligação em desenvolvimento** (já em `.env.example`):

```env
Jwt__SigningKeyPath=keys/oidc-signing.pem
Jwt__SigningKeyPem=
Jwt__SigningKeyPemBase64=
Jwt__Issuer=http://localhost:5000
```

- `Jwt__Issuer` deve coincidir com a URL que o **navegador** usa para a API (`http://localhost:5000` com `API_PORT=5000` padrão).
- Dentro do container o path é `keys/oidc-signing.pem`; no host é `backend/keys/oidc-signing.pem`.

**Produção:** codifique o mesmo PEM em Base64 (veja [§7](#7-deploy-em-produção-docker-compose)); defina `Jwt__SigningKeyPemBase64` e deixe `Jwt__SigningKeyPath` vazio.

### 3.3 Credenciais do admin raiz (bootstrap)

Defina o primeiro administrador em `backend/.env`:

```env
Bootstrap__AdminEmail=admin@localhost
Bootstrap__AdminPassword=SuaSenhaSegura@123
Bootstrap__AdminDisplayName=Admin
```

> Nunca commite senhas reais. Após o primeiro login em produção, remova `Bootstrap__*` do ambiente (veja §7).

### 3.4 Aplicar migrations (no host)

A imagem de desenvolvimento não executa o bundle de migrations EF. Aplique migrations **na sua máquina**.

1. Defina `Database:ConnectionString` em `Kyvo.API/appsettings.Development.json` com **`localhost`** (não `host.docker.internal`). Ajuste usuário, senha e nome do banco conforme seu PostgreSQL.
2. Execute:

```bash
cd backend

dotnet ef database update \
  --project Kyvo.Infrastructure \
  --startup-project Kyvo.API
```

O `dotnet ef` carrega a configuração via `ApplicationDbContextFactory`, que lê **`appsettings.Development.json`** (e opcionalmente `Database__ConnectionString` no ambiente). Ele **não** lê `backend/.env`.

Substituição pontual opcional:

```bash
# Linux / macOS
export Database__ConnectionString="Host=localhost;Port=5432;Database=kyvo_db;Username=postgres;Password=postgrespassword"
```

```powershell
# Windows (PowerShell)
$env:Database__ConnectionString = "Host=localhost;Port=5432;Database=kyvo_db;Username=postgres;Password=postgrespassword"
```

Isso cria todas as tabelas (`AspNetUsers`, entidades `OpenIddict*`, `identity_providers`, `tenants`, `applications`, `application_clients`, `auth_sessions`, `audit_logs`, etc.).

### 3.5 Subir a API

```bash
cd backend
docker compose up -d --build
```

A API fica em `http://localhost:5000` (`API_PORT` padrão). Swagger: `http://localhost:5000/swagger`.

Confirme que está saudável:

```bash
curl http://localhost:5000/api/v1/platform/status
# Esperado: { "isConfigured": true, "requiresBootstrap": false, "oauthClientId": "platform-admin-web" }
```

Na primeira subida, a API inicializa a plataforma (admin, IdP local, client OAuth) com `Bootstrap__*` do `.env`. Se faltar configuração, o status permanece `requiresBootstrap: true` até ajustar `.env` e reiniciar:

```bash
docker compose restart kyvo.api
```

---

## 4. Configurar e iniciar o frontend

Suba o **backend** (§3.5) antes do frontend.

### 4.1 Preparar o arquivo `.env`

```bash
cd frontend
cp .env.example .env
```

O [frontend/.env.example](./frontend/.env.example) documenta as variáveis usadas por [frontend/docker-compose.yml](./frontend/docker-compose.yml):

```env
FRONTEND_PORT=3000
VITE_API_BASE_URL=http://localhost:5000
VITE_API_VERSION=1.0
VITE_API_TIMEOUT_MS=30000
VITE_OAUTH_CLIENT_ID=platform-admin-web
VITE_OAUTH_REDIRECT_URI=http://localhost:3000/auth/callback
```

Os defaults batem com API na porta `5000` e SPA na `3000`. Altere só se usar outras portas.

### 4.2 Subir o SPA

```bash
cd frontend
docker compose up
```

O painel admin fica em `http://localhost:3000`.

---

## 5. Fazer login

Acesse `http://localhost:3000` (containers da API e do frontend rodando).

A plataforma é inicializada automaticamente na subida da API (com `Bootstrap__*` em `backend/.env`). Na primeira execução bem-sucedida, a API cria:

- Usuário admin com a senha de `Bootstrap__AdminPassword`
- Role de plataforma `plat_admin` atribuída ao admin
- Identity Provider `local` habilitado
- Application `platform-admin` + Client OAuth `platform-admin-web` (fixos, não editáveis via API)

Se `Bootstrap__*` não estiver configurado, `/login` pede para configurar `backend/.env` e reiniciar a API.

Verifique o status:

```bash
curl http://localhost:5000/api/v1/platform/status
# { "isConfigured": true, "requiresBootstrap": false, "oauthClientId": "platform-admin-web" }
```

> Após a inicialização bem-sucedida em produção, remova `Bootstrap__*` do `.env` de deploy. Elas não têm mais efeito.

### Login

1. Clique em **"Entrar na plataforma"**
2. Você será redirecionado para `/account/login` no backend (página Blazor SSR; provedores federados redirecionam via `/login/federated/{alias}`)
3. Informe email e senha de `Bootstrap__*` em `backend/.env` (ex.: `admin@localhost` / `SuaSenhaSegura@123`)
4. Após autenticar, o backend redireciona para o callback OIDC
5. O frontend salva os tokens e você acessa o painel

### Self-registration (novos usuários)

Para usuários que ainda NÃO têm conta na plataforma (cenário comum SaaS):

1. A partir de qualquer app cliente (ex.: Pulse CRM) o usuário clica em "Entrar" e é redirecionado para `/connect/authorize`.
2. A página de login do IdP exibe o link **Criar conta** apontando para `/account/register`.
3. O usuário preenche email, senha (respeitando `PasswordPolicy`) e nome. O endpoint é rate-limited pela policy `account_register`.
4. Após o sucesso a plataforma cria um usuário Identity e autentica via cookie — NÃO cria tenant nem membership ainda.
5. O usuário é redirecionado de volta para `/connect/authorize`; o app cliente recebe o `code` OIDC.
6. O app detecta ausência de contexto de tenant no access token OIDC e dispara o fluxo de onboarding. O BFF chama `POST /api/v1/auth/subscribe` com tenant + plano; a resposta inclui um **tenant JWT** (`accessToken`, `token_use=tenant`). Armazene via `switchTenant` / `session.saveTenantToken` — não dependa de refresh OIDC para `tid`.
7. Para atualizar metadados do tenant depois, use `PATCH /api/v1/Tenants/{id}` (somente nome; `tenantKey` é imutável). Para sair de uma aplicação, chame `DELETE /api/v1/auth/account` no contexto da sessão OAuth — owners fazem hard delete do tenant quando não há pendências; o usuário global só é removido quando não restam memberships ativas.

Esse modelo central significa que apps cliente NUNCA implementam tela própria de cadastro; a coleta de senha acontece apenas no domínio do IdP.

---

## 6. Próximos passos

### Criar um tenant

No painel, vá em **Tenants** → **Criar tenant**. Informe nome e chave única (ex: `minha-org`).

### Convidar membros

Dentro de um tenant, acesse **Tenants** → **Convidar membro** (ou **Membros** para gerenciar convites pendentes). O AWS SES deve estar configurado em `Email.*` — se o envio falhar, **nenhum convite é persistido**. Em caso de sucesso, a API retorna `acceptPath` (ex.: `/accept-invite?token=…`); copie a URL completa no console ou em **Membros** → convites pendentes.

### Registrar uma application OAuth

Vá em **Applications** → **Nova application**. Após criar, acesse os detalhes e registre um **Client OAuth** com as redirect URIs da sua aplicação consumidora.

### Adicionar provedores de identidade externos (opcional)

Como platform admin, acesse **Identity Providers** → **Adicionar IdP**. O provedor `local` (bootstrap) permanece habilitado para email/senha.

Os segredos OAuth dos IdPs (`clientSecret` em `ConfigJson`) são armazenados **criptografados em repouso** via ASP.NET Core Data Protection. Os valores em texto puro só são informados na criação/edição e nunca são retornados em endpoints `GET`.

#### Capabilities

Cada identity provider declara uma ou mais flags `IdpCapability`. O formulário admin oferece checkboxes:

| Capability | Permitido em | Política de conflito |
|------------|--------------|----------------------|
| `LocalPassword` | Apenas `Local` (hard-lock) | Somente **um** provider ativo pode anunciá-la. Tentar adicionar segundo falha. |
| `GoogleSocial` | Google, GenericOidc | Adicionar segundo provider habilitado retorna `warnings` mas é aceito. |
| `MicrosoftSocial` | Microsoft, GenericOidc | Warning em conflito. |
| `AppleSocial` | GenericOidc | Warning em conflito. |
| `GenericOidc` | GenericOidc | Warning em conflito. |

O hard-lock para `LocalPassword` espelha a prática de IdPs corporativos (Microsoft Entra, etc.): uma única fonte de email/senha mantém account linking determinístico e evita ambiguidade na UI ("qual formulário é o legítimo?"). Os socials são mais flexíveis: cenários legítimos multi-realm rodam dois Google em paralelo; o warning só sinaliza ao admin para conferir.

#### Federação OAuth redirect (Google, Microsoft, GitHub, OIDC genérico)

Provedores externos usam **redirect OAuth** (OpenIddict Client), não popup nem POST de id_token. A configuração usa o schema `FederatedProviderConfig` em `ConfigJson`:

| Campo | Obrigatório | Para quê |
|-------|-------------|----------|
| `clientId` | Sim | Client id OAuth no provedor upstream |
| `clientSecret` | Sim | Segredo OAuth (criptografado em repouso) |
| `issuer` | Só GenericOidc | URL do issuer para discovery (provedores conhecidos resolvem via preset) |

Passos:

1. Crie um app OAuth no Google Cloud / Azure / GitHub (ou seu issuer OIDC) com redirect URI `https://<host-kyvo>/callback/login/<alias>`.
2. Painel admin (`http://localhost:3000`) → **Identity Providers** → **Adicionar IdP** → tipo **Google** (ou Microsoft/GitHub/GenericOidc), alias ex. `google`, informe `clientId` + `clientSecret` → **Habilitado**.
3. Manter IdP `local` habilitado (bootstrap).
4. Teste: qualquer app OIDC → redirect → `/account/login` → **Continuar com Google** → OAuth upstream → `/callback/login/google` → cookie de sessão → `returnUrl` OAuth.

**Pulse CRM com Google:** o CRM não integra Google diretamente; redireciona para o OIDC da plataforma. Com IdP Google habilitado, em `/account/login` o usuário entra via redirect, volta ao CRM com `code`, faz onboarding/subscribe, atualiza tokens para claims `tid`/`mid` e usa a API normalmente. Ver `samples/pulse-crm/backend/README.md`.

### Integrar uma aplicação consumidora

1. Registre uma **Application** e um **Client OAuth** no painel (redirect URIs da sua app).
2. Use a discovery URL: `http://localhost:5000/.well-known/openid-configuration` (em produção, substitua pelo host público da API).
3. Implemente authorization code + PKCE no seu cliente (SPA, backend, etc.).

---

## 7. Deploy em produção (Docker Compose)

Implante o Kyvo com **duas imagens publicadas** (API + SPA admin). TLS e roteamento por path ficam em um **proxy reverso externo** (Traefik no Coolify, nginx, etc.). Não é necessário clonar este repositório, exceto opcionalmente para gerar a chave OIDC (veja [§3.2](#32-chave-de-assinatura-oidc-rsa)).

**PostgreSQL e Redis são obrigatórios** e não estão no exemplo de compose da aplicação abaixo.

### Desenvolvimento vs produção

| | Desenvolvimento (este repo) | Produção (`kyvo-deploy/`) |
|---|----------------------------|---------------------------|
| Compose | [backend/docker-compose.yml](./backend/docker-compose.yml) | `docker-compose.yml` na pasta de deploy (snippet abaixo) |
| Imagem API | Build local (`Kyvo.API/Dockerfile`) | `mrffilipe/kyvo-api:${IMAGE_TAG}` |
| Frontend | [frontend/docker-compose.yml](./frontend/docker-compose.yml) (Vite dev) | `mrffilipe/kyvo-frontend:${IMAGE_TAG}` |
| Chave JWT | `Jwt__SigningKeyPath` + volume do PEM | Somente `Jwt__SigningKeyPemBase64` (sem montar PEM) |
| Migrations | `dotnet ef` no host (§3.4) | `Database__ApplyMigrationsOnStartup=true` (entrypoint da imagem de produção) |
| Config | `backend/.env` de [backend/.env.example](./backend/.env.example) | `.env` ao lado do `docker-compose.yml` de deploy |

### Pré-requisitos

| Ferramenta | Finalidade |
|------------|------------|
| Docker Engine + Docker Compose v2 | Executar containers |
| PostgreSQL + Redis | Acessíveis pelo container da API (`host.docker.internal` ou hostnames gerenciados) |
| Imagens no Docker Hub | `mrffilipe/kyvo-api:<tag>` e `mrffilipe/kyvo-frontend:<tag>` (`IMAGE_TAG` no `.env`) |
| Proxy reverso com HTTPS | Um host público roteando paths da API e do SPA (veja abaixo) |

Não é necessário .NET SDK nem Node.js no host, salvo para gerar a chave OIDC a partir deste repo.

### Uma URL pública (recomendado)

Com `Jwt__Issuer=https://auth.exemplo.com` e TLS nesse host, usuários e o SPA usam a **mesma origem**. O proxy encaminha paths da API para `kyvo-api` e o restante para `kyvo-frontend`:

| O que você abre ou chama | URL | Destino |
|--------------------------|-----|---------|
| Painel admin (SPA) | `https://auth.exemplo.com/` | `kyvo-frontend` (nginx :80) |
| Callback OAuth | `https://auth.exemplo.com/auth/callback` | `kyvo-frontend` |
| API (JSON, OIDC, login) | `https://auth.exemplo.com/api/v1/...`, `/connect/...`, `/account/...`, `/.well-known/...`, `/swagger`, `/css/...`, `/js/...` | `kyvo-api` (:8080) |

Defina **`Jwt__Issuer`** exatamente como a URL do navegador (esquema + host, sem barra no final). A imagem do frontend é buildada com `VITE_*` vazios para usar `window.location.origin` em runtime.

**Prefixos de path da API** (não devem ir para o container do SPA):

- `/api/v1/`, `/connect/`, `/account/`, `/.well-known/`, `/swagger`, `/css/`, `/js/`, `/brand/` (ex.: `account-theme.js`, `firebase-google-signin.js`)

### Diretório de deploy sugerido

Crie uma pasta fora deste repositório (ex.: `kyvo-deploy/`) com:

```
kyvo-deploy/
  docker-compose.yml
  .env
```

### PostgreSQL e Redis (infra)

Salve como `docker-compose.infra.yml` na pasta de deploy (ou use serviços gerenciados).

```yaml
# PostgreSQL + Redis locais sugeridos (não versionados no Kyvo)
services:
  postgres:
    image: postgres:16-alpine
    restart: unless-stopped
    environment:
      POSTGRES_USER: ${POSTGRES_USER:-postgres}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-postgrespassword}
      POSTGRES_DB: ${POSTGRES_DB:-kyvo_db}
    ports:
      - "${POSTGRES_PORT:-5432}:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-postgres} -d ${POSTGRES_DB:-kyvo_db}"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    restart: unless-stopped
    command: >
      redis-server
      --requirepass ${REDIS_PASSWORD:-default_password}
      --appendonly yes
    ports:
      - "${REDIS_PORT:-6379}:6379"
    volumes:
      - redisdata:/data

volumes:
  pgdata:
  redisdata:
```

Exemplo de `.env` para o snippet (mesmo diretório do arquivo acima):

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgrespassword
POSTGRES_DB=kyvo_db
POSTGRES_PORT=5432
REDIS_PASSWORD=default_password
REDIS_PORT=6379
```

Subir a infra:

```bash
docker compose -f docker-compose.infra.yml --env-file .env.infra up -d
```

| Variável de infra | Padrão sugerido | Uso |
|-------------------|-----------------|-----|
| `POSTGRES_USER` | `postgres` | Usuário do banco |
| `POSTGRES_PASSWORD` | (definir senha forte) | Senha do banco |
| `POSTGRES_DB` | `kyvo_db` | Nome do banco |
| `POSTGRES_PORT` | `5432` | Porta no host |
| `REDIS_PASSWORD` | (definir senha forte) | Senha do Redis |
| `REDIS_PORT` | `6379` | Porta no host |

Alinhe `Database__ConnectionString` e `Redis__ConnectionString` no `.env` com esses valores (o exemplo abaixo usa `host.docker.internal` quando a infra publica portas no host).

### `docker-compose.yml` (produção)

Salve como `docker-compose.yml` na pasta de deploy:

```yaml
# Kyvo — imagens separadas (API + SPA). TLS no proxy externo.

services:
  api:
    image: mrffilipe/kyvo-api:${IMAGE_TAG:-latest}
    container_name: kyvo-api
    restart: unless-stopped
    env_file:
      - path: .env
        required: true
    extra_hosts:
      - "host.docker.internal:host-gateway"
    ports:
      - "${API_PORT:-8080}:8080"
    volumes:
      - app-dataprotection:/app/keys/data-protection

  frontend:
    image: mrffilipe/kyvo-frontend:${IMAGE_TAG:-latest}
    container_name: kyvo-frontend
    restart: unless-stopped
    ports:
      - "${FRONTEND_PORT:-8081}:80"

volumes:
  app-dataprotection:
```

### `.env` (aplicação)

Salve como `.env` ao lado de `docker-compose.yml`:

```env
# Imagens publicadas (Docker Hub)
IMAGE_TAG=3.0.0
API_PORT=8080
FRONTEND_PORT=8081

Database__ConnectionString=Host=host.docker.internal;Port=5432;Database=kyvo_db;Username=postgres;Password=postgrespassword
Database__ApplyMigrationsOnStartup=true

Jwt__Issuer=https://auth.exemplo.com
Jwt__Audience=kyvo-api
Jwt__KeyId=default
Jwt__RefreshTokenDays=30
# Produção: chave RSA somente em Base64 — veja "Chave OIDC (produção)" abaixo.
Jwt__SigningKeyPath=
Jwt__SigningKeyPem=
Jwt__SigningKeyPemBase64=

Redis__ConnectionString=host.docker.internal:6379,password=default_password,ssl=false
Redis__InstanceName=kyvo:
Redis__TenantIdentifierCacheMinutes=5

SecretProtection__KeyDirectoryPath=keys/data-protection
SecretProtection__ApplicationName=Kyvo

Bootstrap__AdminEmail=admin@example.com
Bootstrap__AdminPassword=ChangeMe_Strong_Password_12
Bootstrap__AdminDisplayName=Admin

Email__FromAddress=noreply@example.com
Email__Region=us-east-1
Email__AccessKeyId=
Email__SecretAccessKey=
Email__SessionToken=
```

### Chave OIDC (produção)

1. **Gerar** o PEM (mesmos comandos de [§3.2](#32-chave-de-assinatura-oidc-rsa)) em máquina confiável. Guarde `oidc-signing.pem` com segurança; não commite nem inclua na imagem.
2. **Codificar** o PEM em Base64 em uma linha:

```bash
openssl base64 -A -in oidc-signing.pem
# PowerShell: [Convert]::ToBase64String([IO.File]::ReadAllBytes("oidc-signing.pem"))
```

3. **Definir** no `.env` de deploy:

```env
Jwt__SigningKeyPemBase64=<cole-o-base64-aqui>
Jwt__SigningKeyPath=
Jwt__SigningKeyPem=
```

Em produção use **apenas** `Jwt__SigningKeyPemBase64`. Deixe `Jwt__SigningKeyPath` e `Jwt__SigningKeyPem` vazios para evitar erro na subida.

ASP.NET Core usa `Section__Property`. Em produção **não** defina `VITE_*` no `.env` — a imagem do frontend usa **mesma origem** quando buildada sem build-args customizados.

| Variável | Rebuild da imagem? | Notas |
|----------|-------------------|-------|
| `Database__*`, `Redis__*`, `Jwt__*`, `Bootstrap__*`, `Email__*` | Não | Edite `.env`, depois `docker compose restart api` |
| `Jwt__Issuer` | Não | Deve coincidir com a URL pública |
| Código da plataforma | Sim | Novas tags `kyvo-api` e `kyvo-frontend` com o mesmo `IMAGE_TAG` |

Para **hosts separados** (API e UI), rebuild de `kyvo-frontend` com `--build-arg VITE_API_BASE_URL=...` e `VITE_OAUTH_REDIRECT_URI=...`.

### Passos de deploy

1. Subir PostgreSQL e Redis (snippet de infra ou gerenciados).
2. Gerar `oidc-signing.pem` ([§3.2](#32-chave-de-assinatura-oidc-rsa)).
3. Codificar em Base64 e definir `Jwt__SigningKeyPemBase64` no `.env` (deixe `Jwt__SigningKeyPath` vazio).
4. Criar os arquivos de deploy; configurar o proxy (HTTPS + roteamento por path).
5. Definir `Jwt__Issuer` com a URL pública `https://`.
6. Subir a stack:

```bash
cd kyvo-deploy
docker compose --env-file .env up -d
```

7. Abrir `https://seu-host-publico` (a API inicializa automaticamente na subida). Após confirmar login, remover `Bootstrap__*` e reiniciar:

```bash
docker compose --env-file .env restart api
```

### Problemas comuns (produção)

| Problema | Solução |
|----------|---------|
| Não conecta ao banco | Verificar PostgreSQL e `Database__ConnectionString` |
| API cai ou unhealthy | `docker logs kyvo-api` — `Jwt__SigningKeyPemBase64` inválido ou ausente |
| API reinicia: "Configure only one of Jwt:SigningKeyPath…" | `Jwt__SigningKeyPemBase64` + `SigningKeyPath` do appsettings ao mesmo tempo | Use só Base64; defina `Jwt__SigningKeyPath=` vazio no Coolify. Imagens ≥1.0.1 já vêm com path vazio no template. |
| Redirect OAuth incorreto | `Jwt__Issuer` = URL do navegador; redirect `https://<host>/auth/callback` |
| 404 em `/connect` ou `/account` | Proxy deve rotear prefixos da API para `kyvo-api`, não para o frontend |
| 404 em `firebase-google-signin.js` / `account-theme.js` | `/js/` não vai para a API | Inclua `PathPrefix(\`/js\`)` na regra Traefik da API |
| SPA chama API errada | `Jwt__Issuer` incorreto ou roteamento do proxy | Igualar `Jwt__Issuer` à URL do navegador; verificar paths da API no proxy |

---

## 8. Configuração para produção

### Variáveis de ambiente críticas

| Variável de ambiente (`__`) | Produção |
|-----------------------------|----------|
| `Database__ConnectionString` | String de conexão ao banco gerenciado (RDS, Cloud SQL, etc.) |
| `Jwt__SigningKeyPemBase64` | PEM da chave privada RSA codificado em Base64 (produção; sem montar arquivo) |
| `Jwt__Issuer` | URL pública do backend (ex: `https://auth.meusite.com`) |
| `Bootstrap__AdminEmail` | Apenas no primeiro deploy; remover após bootstrap |
| `Bootstrap__AdminPassword` | Apenas no primeiro deploy; remover após bootstrap |
| `Bootstrap__AdminDisplayName` | Opcional no primeiro deploy |
| `Email__FromAddress`, `Email__Region`, etc. | Configuração AWS SES para convites |
| `Redis__ConnectionString` | Cache distribuído (ElastiCache, Redis Cloud, etc.) |
| `SecretProtection__KeyDirectoryPath` | Diretório persistente para o keyring do data protection (precisa sobreviver a restarts e ser backup) |
| `SecretProtection__ApplicationName` | Nome lógico para isolar o keyring (default `Kyvo`) |
No `appsettings.json` de produção, o equivalente usa `:` (ex.: `Database:ConnectionString`).

### Frontend em produção

O SPA admin roda em `mrffilipe/kyvo-frontend` (nginx na porta 80, HTTP). Configure `Jwt__Issuer` no `.env` da API (seção 7) e roteie o host público no proxy. Para hosts separados, rebuild do frontend com build-args `VITE_*`.

### HTTPS

Em produção, toda comunicação deve ser via HTTPS. O `Jwt:Issuer` deve usar `https://` para que o OIDC funcione corretamente.

---

## 9. Referência rápida de comandos

```bash
# Backend: preparar .env e subir API (desenvolvimento)
cd backend && cp .env.example .env
cd backend && docker compose up -d --build
cd backend && docker compose logs -f kyvo.api
cd backend && docker compose restart kyvo.api

# Backend: aplicar migrations (lê appsettings.Development.json — use localhost em Database)
cd backend
dotnet ef database update --project Kyvo.Infrastructure --startup-project Kyvo.API

# Backend: gerar nova migration
dotnet ef migrations add NomeDaMigration --project Kyvo.Infrastructure --startup-project Kyvo.API --output-dir Migrations

# Frontend: preparar .env e subir SPA (desenvolvimento)
cd frontend && cp .env.example .env
cd frontend && docker compose up

# Chave OIDC (ver §3.2)
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out backend/keys/oidc-signing.pem

# Status da plataforma (após subir a API)
curl http://localhost:5000/api/v1/platform/status
```

---

## 10. Solução de problemas

| Problema | Causa provável | Solução |
|----------|---------------|---------|
| API não inicia: erro de chave RSA | Chave ausente ou múltiplas fontes | Gerar PEM (§3.2); dev: `Jwt__SigningKeyPath` + volume; prod: só `Jwt__SigningKeyPemBase64` |
| Compose: `not a directory` ao montar `oidc-signing.pem` | PEM ausente no primeiro `up` — Docker criou uma pasta com esse nome | Remova `backend/keys/oidc-signing.pem` se for diretório; gere o arquivo PEM (§3.2) e tente de novo |
| API reinicia: "Configure only one of Jwt:SigningKeyPath…" | Base64 e Path definidos juntos | Produção: limpe `Jwt__SigningKeyPath` e `Jwt__SigningKeyPem`; use só `Jwt__SigningKeyPemBase64` |
| `dotnet ef` não conecta | Host ou credenciais errados em `appsettings.Development.json` | Use `Host=localhost` nas migrations; mantenha `host.docker.internal` no `.env` do container. O `dotnet ef` não lê `backend/.env` |
| `dotnet ef` falha na senha mesmo com `.env` correto | EF usa `ApplicationDbContextFactory` + appsettings, não `.env` | Ajuste `Database:ConnectionString` em `appsettings.Development.json` (ou exporte `Database__ConnectionString`) |
| Container da API não alcança PostgreSQL | Host errado em `backend/.env` | Use `host.docker.internal` em `Database__ConnectionString` quando o banco está no host |
| Plataforma não inicializada (`requiresBootstrap: true`) | `Bootstrap__*` ausente em `backend/.env` | Defina `Bootstrap__AdminEmail` / `Bootstrap__AdminPassword` e `docker compose restart kyvo.api` |
| Frontend não carrega após login | `VITE_OAUTH_REDIRECT_URI` incorreta | Confirme `redirect_uri` do `platform-admin-web` em `frontend/.env` |
| JWT expirado / 401 | Token expirado e refresh falhou | Fazer logout e login novamente |
| Convites não chegam por email | AWS SES não configurado | Configurar `Email__*` com credenciais SES válidas no `.env` |
| Erro de CORS | Frontend em URL diferente | Verificar `VITE_API_BASE_URL` em `frontend/.env` |
| Não decripta IdP existente | Keyring do Data Protection perdido | Restaurar `SecretProtection__KeyDirectoryPath` do backup ou recriar o IdP |
| Docker: não conecta ao PostgreSQL/Redis | Infra parada ou strings erradas | Subir infra ou serviços gerenciados; conferir `Database__*` e `Redis__*` no `.env` de deploy |
| Docker: erro de redirect OAuth | `Jwt__Issuer` ou redirect do client OAuth | `Jwt__Issuer` = URL pública; client `platform-admin-web` com `https://<host>/auth/callback` |
| Docker: HTTPS / esquema OIDC incorreto | Proxy sem `X-Forwarded-Proto` ou `Jwt__Issuer` incorreto | TLS no proxy; `Jwt__Issuer` com `https://...` |
