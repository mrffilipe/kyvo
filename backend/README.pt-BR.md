# Kyvo — Backend

[English](./README.md) | [Português](./README.pt-BR.md)

> **Pronúncia:** *Kyvo* pronuncia-se como **"Key"vo** — parecido com a palavra inglesa *key* + *vo*.

API .NET 8 que implementa um **Identity Provider (IdP)** completo: autenticação local, OIDC (authorization code + PKCE), multi-tenant, roles, aplicações OAuth e federação de provedores externos.

> Convenções de código: ver [backend/README.md](../backend/README.md).

---

## Arquitetura

A solução segue **Clean Architecture** com 4 projetos:

```
Kyvo.Domain          → Entidades, value objects, interfaces de repositório, regras de domínio
Kyvo.Application     → Use cases, queries, policies, ports, DTOs e requests
Kyvo.Infrastructure  → Implementações: EF Core, OIDC, email (AWS SES), serviços técnicos
Kyvo.API             → Controllers ASP.NET Core, Program.cs, middlewares, views MVC (login)
```

### Camada Application (use cases + queries)

Workflows de negócio são expostos como **use cases** (`I{Action}UseCase.ExecuteAsync`) e leituras como **queries** (`I{Action}Query.ExecuteAsync`). Controllers injetam essas interfaces diretamente.

| Área | Exemplos |
|------|----------|
| **UseCases/** | `ICreateTenantUseCase`, `ISubscribeTenantUseCase`, `IInviteMemberUseCase`, `IDeleteAccountUseCase` |
| **Queries/** | `IGetTenantByIdQuery`, `IListApplicationsQuery`, `IListAuditLogsQuery` |
| **Policies/** | `ITenantAuthorizationPolicy`, `ITenantAccountEligibilityPolicy` |
| **Shared/** | `ITenantProvisioner`, `TenantContextBuilder` |
| **Ports/** | `IUserAccountService`, `IEmailService`, `IApplicationBrandingStorage`, `IKyvoClaimsPrincipalFactory`, `IPlatformBootstrapExecutor` |

DTOs de leitura ficam em `Queries/{Area}/Dtos/`; requests de comando e tipos `*Result` ficam junto ao use case ou query.

### Fluxo de autenticação

```
POST /account/signin (email + senha)
  → Cookie de aplicação do ASP.NET Core Identity + linha AuthSession (claim sid)

GET /connect/authorize (PKCE)
  → OpenIddict valida o cookie Identity, carrega AuthSession, monta claims via IKyvoClaimsPrincipalFactory e emite authorization_code

POST /connect/token (code + verifier)
  → JWT RS256 (access_token + id_token + refresh_token)

Bearer JWT → controllers v1 protegidos
```

O `User` de domínio (perfil/ciclo de vida) é separado do `ApplicationUser` na Infrastructure (`IdentityUser<Guid>`, mesma tabela `users`). A Application usa `IUserAccountService` para senha/logins; o host da API usa `UserManager<ApplicationUser>` / `SignInManager<ApplicationUser>` para sign-in por cookie.

---

## Pré-requisitos

| Ferramenta | Versão |
|------------|--------|
| .NET SDK | 8.0+ |
| PostgreSQL | 14+ |
| Redis | opcional (cache de tenant; sem ele usa in-memory) |
| `dotnet-ef` | `dotnet tool install --global dotnet-ef` |

---

## Configuração

A configuração usa `Kyvo.API/appsettings.json` (template), `appsettings.Development.json` (ferramentas no host) e **`backend/.env`** (container da API no Docker Compose). Veja [GETTING_STARTED.pt-BR.md §3](../GETTING_STARTED.pt-BR.md#3-configurar-o-backend).

| Arquivo | Usado por | Host em `Database` (típico) |
|---------|-----------|----------------------------|
| `appsettings.Development.json` | `dotnet ef` no host (`ApplicationDbContextFactory`) | `localhost` |
| `backend/.env` | `docker compose` (`env_file` em `kyvo.api`) | `host.docker.internal` |

Copie [`.env.example`](./.env.example) para `.env` e mantenha credenciais alinhadas com `appsettings.Development.json`.

### Seções do appsettings

Todo tipo `*Options` tem bind + validação em startup (`IValidateOptions<T>` + `ValidateOnStart()`). Configurações inválidas falham na inicialização. As chaves devem estar presentes no appsettings; veja `Kyvo.API/appsettings.json` (template com comentários inline) e `appsettings.Development.json` (valores locais).

#### `Database`

| Propriedade | Obrigatória | Descrição |
|-------------|-------------|-----------|
| `ConnectionString` | Sim | String de conexão PostgreSQL. Env: `Database__ConnectionString` |

#### `Jwt`

| Propriedade | Obrigatória | Descrição |
|-------------|-------------|-----------|
| `Issuer` | Sim | URI absoluta do issuer (discovery + tokens). Env: `Jwt__Issuer` |
| `Audience` | Sim | Audience do resource da API nos access tokens. Env: `Jwt__Audience` |
| `RefreshTokenDays` | Sim | Validade do refresh token em dias (deve ser > 0). Env: `Jwt__RefreshTokenDays` |
| `SigningKeyPath` | Um dos três | Caminho do arquivo PEM da chave RSA (dev local). Env: `Jwt__SigningKeyPath` |
| `SigningKeyPem` | Um dos três | Texto PEM inline. Env: `Jwt__SigningKeyPem` |
| `SigningKeyPemBase64` | Um dos três | PEM em Base64 (containers). Env: `Jwt__SigningKeyPemBase64` |
| `KeyId` | Sim | Id da chave publicada no JWKS. Env: `Jwt__KeyId` |

Configure exatamente uma fonte de chave (`SigningKeyPath`, `SigningKeyPem` ou `SigningKeyPemBase64`).

#### `Bootstrap`

| Propriedade | Obrigatória | Descrição |
|-------------|-------------|-----------|
| `AdminEmail` | Condicional | Email do admin raiz; obrigatório quando qualquer campo bootstrap é informado. Env: `Bootstrap__AdminEmail` |
| `AdminPassword` | Condicional | Senha inicial do admin; obrigatória quando qualquer campo bootstrap é informado. Env: `Bootstrap__AdminPassword` |
| `AdminDisplayName` | Não | Nome de exibição (padrão: parte local do email). Env: `Bootstrap__AdminDisplayName` |

#### `RateLimit`

| Propriedade | Obrigatória | Descrição |
|-------------|-------------|-----------|
| `AccountRegisterPermitLimit` | Sim | Máximo de tentativas de registro por IP por janela (deve ser > 0) |
| `AccountRegisterWindowMinutes` | Sim | Duração da janela deslizante em minutos (deve ser > 0) |

#### `Invite`

| Propriedade | Obrigatória | Descrição |
|-------------|-------------|-----------|
| `ExpirationHours` | Sim | Horas até um convite pendente expirar (deve ser > 0) |

#### `Email`

| Propriedade | Obrigatória | Descrição |
|-------------|-------------|-----------|
| `FromAddress` | Sim | Endereço remetente verificado no SES. Env: `Email__FromAddress` |
| `Region` | Sim | Região AWS do SES (ex.: `us-east-1`). Env: `Email__Region` |
| `AccessKeyId` | Não | Access key estática; omita para usar credenciais da instância/task role. Env: `Email__AccessKeyId` |
| `SecretAccessKey` | Não | Secret key estática (par com `AccessKeyId`). Env: `Email__SecretAccessKey` |
| `SessionToken` | Não | Token de sessão STS para credenciais temporárias. Env: `Email__SessionToken` |

#### `Redis`

| Propriedade | Obrigatória | Descrição |
|-------------|-------------|-----------|
| `ConnectionString` | Chave obrigatória | Connection string StackExchange.Redis; vazio usa cache em memória. Env: `Redis__ConnectionString` |
| `InstanceName` | Sim | Prefixo de chaves quando Redis está habilitado (ex.: `kyvo:`). Env: `Redis__InstanceName` |
| `TenantIdentifierCacheMinutes` | Sim | TTL do cache de identificador de tenant em minutos (deve ser > 0). Env: `Redis__TenantIdentifierCacheMinutes` |

#### `SecretProtection`

| Propriedade | Obrigatória | Descrição |
|-------------|-------------|-----------|
| `KeyDirectoryPath` | Sim | Diretório do keyring do Data Protection. Env: `SecretProtection__KeyDirectoryPath` |
| `ApplicationName` | Sim | Isola as chaves desta aplicação de outras no mesmo diretório. Env: `SecretProtection__ApplicationName` |

#### `PasswordPolicy`

| Propriedade | Obrigatória | Descrição |
|-------------|-------------|-----------|
| `MinLength` | Sim | Tamanho mínimo da senha (deve ser >= 8) |
| `RequireDigit` | Sim | Exige ao menos um dígito |
| `RequireLetter` | Sim | Exige ao menos uma letra |

Mensagens de erro de validação estão centralizadas em `InfrastructureErrorMessages` (`Kyvo.Infrastructure/Configurations/`).

### Variáveis de ambiente (`.env` de produção Docker)

O ASP.NET Core mapeia `Secao__Propriedade` para `Secao:Propriedade` (equivalente ao JSON aninhado). Exemplo para bootstrap:

| Variável | Obrigatória | Descrição |
|----------|-------------|-----------|
| `Bootstrap__AdminEmail` | Sim | Email do administrador raiz |
| `Bootstrap__AdminPassword` | Sim | Senha inicial (nunca persiste em texto) |
| `Bootstrap__AdminDisplayName` | Não | Nome de exibição (padrão: parte do email) |

Outras chaves comuns: `Database__ConnectionString`, `Jwt__Issuer`, `Jwt__RefreshTokenDays`, `Jwt__SigningKeyPemBase64`, `Redis__ConnectionString`, `Email__FromAddress`, `Email__SessionToken`, `SecretProtection__KeyDirectoryPath`, etc.

Em desenvolvimento local, bootstrap e banco ficam em **`backend/.env`** (container) e **`appsettings.Development.json`** (`dotnet ef`). Veja [GETTING_STARTED.pt-BR.md](../GETTING_STARTED.pt-BR.md).

> Após o bootstrap, remova `Bootstrap__*` do ambiente em produção. Elas só são necessárias na primeira inicialização.

### Imagem Docker da API

Imagem de produção: [`Dockerfile`](./Dockerfile) → `mrffilipe/kyvo-api`. **Deploy:** [../GETTING_STARTED.pt-BR.md § Produção](../GETTING_STARTED.pt-BR.md#7-deploy-em-produção-docker-compose). **Build/push:** [../docs/DOCKER_PUBLISH.pt-BR.md](../docs/DOCKER_PUBLISH.pt-BR.md).

| Tópico | Detalhe |
|--------|---------|
| Porta | `8080` no container |
| Migrations | Bundle EF executado quando `Database__ApplyMigrationsOnStartup=true` |
| Chave JWT (produção) | `Jwt__SigningKeyPemBase64` — PEM em Base64 (sem montar arquivo) |
| Chave JWT (dev local) | `Jwt__SigningKeyPath` — caminho do PEM (ver abaixo); não commitado no appsettings |
| Data Protection | Volume em `/app/keys/data-protection` |
| Health | `GET /v1.0/platform/status` na porta `8080` |
| HTTPS | Forwarded Headers para TLS no proxy reverso externo |

### Chave RSA para OIDC

Os JWTs são assinados com RSA (RS256). Gere uma chave privada de 2048 bits e configure exatamente uma fonte de assinatura antes de subir a API.

**Linux / macOS:**

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out oidc-signing.pem
export Jwt__SigningKeyPath="$PWD/oidc-signing.pem"
```

**Windows** (OpenSSL no PATH):

```powershell
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out oidc-signing.pem
$env:Jwt__SigningKeyPath = (Resolve-Path oidc-signing.pem).Path
```

**Windows** (sem OpenSSL — .NET):

```powershell
$path = Join-Path $env:LOCALAPPDATA 'kyvo\oidc-signing.pem'
New-Item -ItemType Directory -Force -Path (Split-Path $path) | Out-Null
$rsa = [System.Security.Cryptography.RSA]::Create(2048)
[System.IO.File]::WriteAllText($path, $rsa.ExportPkcs8PrivateKeyPem())
$env:Jwt__SigningKeyPath = $path
```

Configure `Jwt:SigningKeyPath` em desenvolvimento local, `Jwt:SigningKeyPem` para PEM inline, ou `Jwt:SigningKeyPemBase64` / `Jwt__SigningKeyPemBase64` em produção (`openssl base64 -A -in oidc-signing.pem`). Use apenas uma fonte por vez. Não commite arquivos PEM.

---

## Proteção de segredos em repouso

O JSON de configuração dos IdPs (`IdentityProvider.ConfigJson`) costuma conter segredos (`clientSecret`, etc.). Os campos sensíveis de nível superior são cifrados antes da persistência via ASP.NET Core Data Protection através de `ISecretProtector` e `IdentityProviderConfigCipher`.

- Payloads ainda em texto puro continuam legíveis em runtime e são re-cifrados na próxima escrita.
- Valores cifrados recebem o prefixo `enc:v1:`.
- O keyring é persistido em `SecretProtection:KeyDirectoryPath` (default `keys/data-protection`). **Perder essas chaves significa perder o acesso aos segredos armazenados** — faça backup junto com o banco.

`IdentityProviderDto` propositalmente omite `ConfigJson`; segredos nunca são devolvidos para consumidores da API.

---

## Build da imagem Docker

Na raiz do repositório:

```bash
docker build -f backend/Dockerfile -t mrffilipe/kyvo-api:<tag> .
```

---

## Como rodar localmente (Docker Compose)

Veja [GETTING_STARTED.pt-BR.md §3–3.5](../GETTING_STARTED.pt-BR.md#3-configurar-o-backend). Resumo:

```bash
cd backend
cp .env.example .env
# Gere backend/keys/oidc-signing.pem (veja GETTING_STARTED §3.2)
# Alinhe Database:ConnectionString em appsettings.Development.json (localhost) e backend/.env (host.docker.internal)

dotnet ef database update \
  --project Kyvo.Infrastructure \
  --startup-project Kyvo.API

docker compose up -d --build
```

O `dotnet ef` lê `appsettings.Development.json` via `ApplicationDbContextFactory` — não o `backend/.env`. O container da API usa `.env` em runtime.

---

## Bootstrap

A plataforma é inicializada automaticamente na subida da API, uma única vez, enquanto `PlatformConfiguration.IsBootstrapped` for `false` e as credenciais `Bootstrap:AdminEmail` / `Bootstrap:AdminPassword` estiverem configuradas.

Configure as credenciais antes de iniciar a API (`Bootstrap__*` em `backend/.env` no dev local, ou variáveis de ambiente em produção). Na subida, a API cria:
- Usuário admin raiz com credenciais locais ASP.NET Core Identity
- Role de plataforma `plat_admin` atribuída ao admin
- Identity Provider `local` habilitado
- Application `platform-admin` + Client `platform-admin-web` (fixos, não editáveis via API)
- Registro de `PlatformConfiguration` marcando o sistema como bootstrapped

Verifique o status:

```bash
curl http://localhost:5000/v1.0/platform/status
# { "isConfigured": true, "requiresBootstrap": false, "oauthClientId": "platform-admin-web" }
```

> Após a inicialização bem-sucedida em produção, remova `Bootstrap__*` do ambiente. Elas não têm mais efeito.

---

## Endpoints principais

### Platform
| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/v1.0/platform/status` | Público | Status e se requer bootstrap |

### Account / OIDC
| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/account/login` | Público | Página de login (Blazor Web App Static SSR) |
| POST | `/account/signin` | Público | Handler de credencial local (cookie sign-in) |
| GET | `/account/register` | Público | Página de self-registration (Blazor SSR) |
| POST | `/account/register` | Público, rate-limited | Handler de cadastro (cria usuário Identity) |
| GET | `/login/federated/{alias}` | Público | Inicia redirect OAuth federado |
| GET/POST | `/callback/login/{alias}` | Público | Callback OAuth federado |
| POST | `/account/logout` | Cookie | Encerrar sessão local |
| GET/POST | `/connect/authorize` | Cookie | Endpoint de autorização OIDC |
| POST | `/connect/token` | Client credentials | Troca de código por token |
| GET/POST | `/connect/userinfo` | Bearer | OIDC userinfo |
| GET/POST | `/connect/logout` | Cookie / Bearer | Logout OIDC |
| GET | `/.well-known/openid-configuration` | Público | Discovery OIDC |
| GET | `/.well-known/jwks.json` | Público | Chaves públicas RSA |

### Auth (JWT)
| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| POST | `/v1.0/auth/subscribe` | JWT | Onboarding SaaS (criar tenant via app OAuth) |
| POST | `/v1.0/auth/switch-tenant` | JWT | Mudar tenant ativo na sessão |
| GET | `/v1.0/auth/sessions` | JWT | Listar sessões ativas |
| DELETE | `/v1.0/auth/sessions/{id}` | JWT | Revogar sessão |
| DELETE | `/v1.0/auth/account` | JWT + contexto de tenant | Excluir conta no tenant da aplicação atual (owner faz hard delete do tenant; demais usuários apenas revogam membership) |

**Metadados do tenant:** use `PATCH /v1.0/Tenants/{id}` para atualizar o nome após `POST /v1.0/auth/subscribe` (`tenantKey` é imutável).

### Users
| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/v1.0/Users` | JWT + admin do tenant ou plat_admin | Buscar usuários por email ou nome (picker) |
| GET | `/v1.0/Users/me` | JWT | Perfil do usuário atual |
| PATCH | `/v1.0/Users/me` | JWT | Atualizar perfil |
| GET | `/v1.0/Users/me/memberships` | JWT | Memberships do usuário |

### Identity Providers
| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/v1.0/IdentityProviders` | JWT + plat_admin | Listar IdPs |
| GET | `/v1.0/IdentityProviders/{id}` | JWT + plat_admin | Obter IdP por id |
| GET | `/v1.0/IdentityProviders/aliases/{alias}/availability` | JWT + plat_admin | Verificar disponibilidade do alias |
| POST | `/v1.0/IdentityProviders` | JWT + plat_admin | Adicionar IdP (campos sensíveis do ConfigJson são cifrados ao salvar) |
| PATCH | `/v1.0/IdentityProviders/{id}` | JWT + plat_admin | Atualizar IdP |
| POST | `/v1.0/IdentityProviders/{id}/enable` | JWT + plat_admin | Habilitar |
| POST | `/v1.0/IdentityProviders/{id}/disable` | JWT + plat_admin | Desabilitar |

### Applications (admin de plataforma)

| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/v1.0/Applications` | JWT + plat_admin | Listar applications |
| GET | `/v1.0/Applications/slugs/{slug}/availability` | JWT + plat_admin | Verificar disponibilidade do slug |
| GET | `/v1.0/Applications/{id}/branding` | JWT + plat_admin | Branding da tela de login |
| PATCH | `/v1.0/Applications/{id}/branding` | JWT + plat_admin | Atualizar cores e textos do hero |
| POST | `/v1.0/Applications/{id}/branding/logo` | JWT + plat_admin | Enviar logo de login |
| DELETE | `/v1.0/Applications/{id}/branding/logo` | JWT + plat_admin | Remover logo de login |

### Audit logs

| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/v1.0/AuditLogs` | JWT + contexto de tenant | Listar audit logs (filtrado) |
| GET | `/v1.0/AuditLogs/filter-options` | JWT + contexto de tenant | Ações/usuários distintos para filtros |

#### Identity Providers federados

A configuração de cada IdP externo fica em `ConfigJson` no banco (cadastro via painel). Todos os tipos federados compartilham `FederatedProviderConfig` (`clientId`, `clientSecret`, `issuer` opcional para GenericOidc).

| Tipo | `ConfigJson` | Login em `/account/login` |
|------|--------------|---------------------------|
| `Local` | opcional / vazio | email + senha |
| `Google` | `clientId`, `clientSecret` | redirect OAuth via `/login/federated/{alias}` |
| `Microsoft` | `clientId`, `clientSecret` | redirect OAuth |
| `GitHub` | `clientId`, `clientSecret` | redirect OAuth |
| `GenericOidc` | `clientId`, `clientSecret`, `issuer` | redirect OAuth |

Fluxo OIDC: o painel admin inicia `connect/authorize` → redirect para `/account/login` → métodos exibidos conforme IdPs **habilitados** → cookie de sessão → retorno ao cliente.

Login federado usa OpenIddict Client: `/login/federated/{alias}` redireciona ao provedor upstream; `/callback/login/{alias}` conclui o fluxo e define o cookie antes de continuar o `returnUrl` OAuth.

Registre redirect URIs no provedor upstream como `https://<host-kyvo>/callback/login/<alias>`.

### Tenants (destaques)

| Método | Path | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/v1.0/Tenants/keys/{key}/availability` | JWT | Verificar disponibilidade da tenant key |
| POST | `/v1.0/Tenants/{id}/invites` | JWT (owner/admin/plat_admin) | Enviar convite; persiste só após SES ok; retorna `id` + `acceptPath` |
| GET | `/v1.0/Tenants/{id}/invites` | JWT (owner/admin/plat_admin) | Listar convites (`acceptPath` em pendentes com token cifrado) |
| DELETE | `/v1.0/Invites/{id}` | JWT (owner/admin/plat_admin) | Revogar convite pendente |
| POST | `/v1.0/invites/accept` | JWT | Aceitar convite por token |

Tokens são armazenados com hash (`token_hash`) e cifrados em repouso (`encrypted_token` via Data Protection) para permitir copiar links de convites pendentes. Convites legados sem `encrypted_token` listam com `acceptPath: null`.

Memberships e demais CRUD de applications: ver `frontend/swagger.json`.

---

## Autorização

- **Claim `prole=plat_admin`**: administrador de plataforma. Resolvida consultando `UserPlatformRole` + `PlatformRole` no banco.
- **Policy `PlatformAdministrator`**: protege criação de tenants, applications, gestão de IdPs.
- **`trole`**: papéis do tenant ativo (owner, admin, member, viewer).
- **Tenant context**: claims `tid` (tenant id) e `mid` (membership id) no JWT.

---

## Entidades de domínio

| Entidade | Tabela | Descrição |
|----------|--------|-----------|
| `User` | `users` | Usuário da plataforma (vinculado a `AspNetUsers`) |
| `UserPlatformRole` | `user_platform_roles` | Atribuição de role de plataforma |
| `PlatformRole` | `platform_roles` | Papéis globais (ex: `plat_admin`) |
| `IdentityProvider` | `identity_providers` | Configuração de IdP (Local, Google, Microsoft, GitHub, GenericOidc) |
| `Tenant` | `tenants` | Organização / espaço isolado |
| `TenantRole` | `tenant_roles` | Papéis configuráveis por tenant |
| `TenantMembership` | `tenant_memberships` | Vínculo usuário ↔ tenant |
| `Application` | `applications` | Aplicação OAuth registrada |
| `ApplicationClient` | `application_clients` | Client OAuth (public/confidential) |
| `ApplicationTenant` | `application_tenants` | Vínculo app ↔ tenant (provisioning) |
| `AuthSession` | `auth_sessions` | Sessão ativa (vincula cookie a JWT) |
| `AuditLog` | `audit_logs` | Registro de eventos por tenant |
| `TenantInvite` | `tenant_invites` | Convite de membro para tenant |

---

## Migrations

Em desenvolvimento local, o `dotnet ef` lê `Database:ConnectionString` em `Kyvo.API/appsettings.Development.json` via `ApplicationDbContextFactory` (use `localhost` como host do banco). O container da API usa `backend/.env`. Veja [GETTING_STARTED.pt-BR.md §3.4](../GETTING_STARTED.pt-BR.md#34-aplicar-migrations-no-host).

```bash
# Gerar nova migration
dotnet ef migrations add NomeDaMigration \
  --project Kyvo.Infrastructure \
  --startup-project Kyvo.API \
  --output-dir Migrations

# Aplicar ao banco
dotnet ef database update \
  --project Kyvo.Infrastructure \
  --startup-project Kyvo.API

# Remover última migration (não aplicada)
dotnet ef migrations remove \
  --project Kyvo.Infrastructure \
  --startup-project Kyvo.API
```
