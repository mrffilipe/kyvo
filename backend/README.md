# Kyvo — Backend

[English](./README.md) | [Português](./README.pt-BR.md)

> **Pronunciation:** *Kyvo* is pronounced like **"Key"vo** — rhymes with English *key* plus *vo*.

A .NET 8 API that implements a full **Identity Provider (IdP)**: local authentication, OIDC (authorization code + PKCE), multi-tenancy, roles, OAuth applications, and federation with external providers.

> Coding conventions: see [backend/README.md](../backend/README.md).

---

## Architecture

The solution follows **Clean Architecture** with 4 projects:

```
Kyvo.Domain          → Entities, value objects, repository interfaces, domain rules
Kyvo.Application     → Use cases, queries, policies, ports, DTOs e requests
Kyvo.Infrastructure  → Implementations: EF Core, OIDC, email (AWS SES), technical services
Kyvo.API             → ASP.NET Core controllers, Program.cs, middlewares, MVC views (login)
```

### Application layer (use cases + queries)

Business workflows are exposed as **use cases** (`I{Action}UseCase.ExecuteAsync`) and reads as **queries** (`I{Action}Query.ExecuteAsync`). Controllers inject these directly.

| Area | Examples |
|------|----------|
| **UseCases/** | `ICreateTenantUseCase`, `ISubscribeTenantUseCase`, `IInviteMemberUseCase`, `IDeleteAccountUseCase` |
| **Queries/** | `IGetTenantByIdQuery`, `IListApplicationsQuery`, `IListAuditLogsQuery` |
| **Policies/** | `ITenantAuthorizationPolicy`, `ITenantAccountEligibilityPolicy` |
| **Shared/** | `ITenantProvisioner`, `TenantContextBuilder` |
| **Ports/** | `IUserAccountService`, `IEmailService`, `IApplicationBrandingStorage`, `IKyvoClaimsPrincipalFactory`, `IPlatformBootstrapExecutor` |

Read DTOs live under `Queries/{Area}/Dtos/`; command requests and `*Result` types sit next to their use case or query.

### Authentication flow

```
POST /account/signin (email + password)
  → ASP.NET Core Identity application cookie + AuthSession row (sid claim)

GET /connect/authorize (PKCE)
  → OpenIddict validates the Identity cookie, loads AuthSession, builds claims via IKyvoClaimsPrincipalFactory, issues authorization_code

POST /connect/token (code + verifier)
  → JWT RS256 (access_token + id_token + refresh_token)

Bearer JWT → protected v1 controllers
```

Domain `User` (profile/lifecycle) is separate from Infrastructure `ApplicationUser` (`IdentityUser<Guid>`, same `users` table). Application code uses `IUserAccountService` for password/login operations; the API host uses `UserManager<ApplicationUser>` / `SignInManager<ApplicationUser>` for cookie sign-in.

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| PostgreSQL | 14+ |
| Redis | optional (tenant cache; falls back to in-memory) |
| `dotnet-ef` | `dotnet tool install --global dotnet-ef` |

---

## Configuration

All configuration lives in `Kyvo.API/appsettings.json` (template) and `appsettings.Development.json` (local development values).

### Appsettings sections

Every `*Options` type is bound and validated at startup (`IValidateOptions<T>` + `ValidateOnStart()`). Misconfigured deployments fail fast. Property keys must be present in appsettings; see `Kyvo.API/appsettings.json` (template with inline comments) and `appsettings.Development.json` (local values).

#### `Database`

| Property | Required | Description |
|----------|----------|-------------|
| `ConnectionString` | Yes | PostgreSQL connection string. Env: `Database__ConnectionString` |

#### `Jwt`

| Property | Required | Description |
|----------|----------|-------------|
| `Issuer` | Yes | Absolute issuer URI (discovery + tokens). Env: `Jwt__Issuer` |
| `Audience` | Yes | API resource audience for access tokens. Env: `Jwt__Audience` |
| `RefreshTokenDays` | Yes | Refresh token lifetime in days (must be > 0). Env: `Jwt__RefreshTokenDays` |
| `SigningKeyPath` | One of three | RSA private key PEM file path (local dev). Env: `Jwt__SigningKeyPath` |
| `SigningKeyPem` | One of three | Inline PEM text. Env: `Jwt__SigningKeyPem` |
| `SigningKeyPemBase64` | One of three | Base64-encoded PEM (containers). Env: `Jwt__SigningKeyPemBase64` |
| `KeyId` | Yes | Key id published in JWKS. Env: `Jwt__KeyId` |

Configure exactly one signing key source (`SigningKeyPath`, `SigningKeyPem`, or `SigningKeyPemBase64`).

#### `Bootstrap`

| Property | Required | Description |
|----------|----------|-------------|
| `AdminEmail` | Conditional | Root admin email; required when any bootstrap field is set. Env: `Bootstrap__AdminEmail` |
| `AdminPassword` | Conditional | Initial admin password; required when any bootstrap field is set. Env: `Bootstrap__AdminPassword` |
| `AdminDisplayName` | No | Display name (defaults to the local part of the email). Env: `Bootstrap__AdminDisplayName` |

#### `RateLimit`

| Property | Required | Description |
|----------|----------|-------------|
| `AccountRegisterPermitLimit` | Yes | Max self-registration attempts per IP per window (must be > 0) |
| `AccountRegisterWindowMinutes` | Yes | Sliding window length in minutes (must be > 0) |

#### `Invite`

| Property | Required | Description |
|----------|----------|-------------|
| `ExpirationHours` | Yes | Hours until a pending tenant invite expires (must be > 0) |

#### `Email`

| Property | Required | Description |
|----------|----------|-------------|
| `FromAddress` | Yes | Verified SES sender address. Env: `Email__FromAddress` |
| `Region` | Yes | AWS region for SES (e.g. `us-east-1`). Env: `Email__Region` |
| `AccessKeyId` | No | Static AWS access key; omit to use instance/task role credentials. Env: `Email__AccessKeyId` |
| `SecretAccessKey` | No | Static AWS secret key (pair with `AccessKeyId`). Env: `Email__SecretAccessKey` |
| `SessionToken` | No | STS session token for temporary credentials. Env: `Email__SessionToken` |

#### `Redis`

| Property | Required | Description |
|----------|----------|-------------|
| `ConnectionString` | Key required | StackExchange.Redis connection string; leave empty to use in-memory cache. Env: `Redis__ConnectionString` |
| `InstanceName` | Yes | Key prefix when Redis is enabled (e.g. `kyvo:`). Env: `Redis__InstanceName` |
| `TenantIdentifierCacheMinutes` | Yes | Tenant identifier cache TTL in minutes (must be > 0). Env: `Redis__TenantIdentifierCacheMinutes` |

#### `SecretProtection`

| Property | Required | Description |
|----------|----------|-------------|
| `KeyDirectoryPath` | Yes | Filesystem directory for the Data Protection keyring. Env: `SecretProtection__KeyDirectoryPath` |
| `ApplicationName` | Yes | Isolates this app's keys from others sharing the directory. Env: `SecretProtection__ApplicationName` |

#### `PasswordPolicy`

| Property | Required | Description |
|----------|----------|-------------|
| `MinLength` | Yes | Minimum password length (must be >= 8) |
| `RequireDigit` | Yes | Require at least one digit |
| `RequireLetter` | Yes | Require at least one letter |

Validation error messages are centralized in `InfrastructureErrorMessages` (`Kyvo.Infrastructure/Configurations/`).

### Environment variables (Docker production `.env`)

ASP.NET Core maps `Section__Property` to `Section:Property` (equivalent to nested JSON). Example for bootstrap:

| Variable | Required | Description |
|----------|----------|-------------|
| `Bootstrap__AdminEmail` | Yes | Root admin email |
| `Bootstrap__AdminPassword` | Yes | Initial password (never stored in plain text) |
| `Bootstrap__AdminDisplayName` | No | Display name (defaults to the local part of the email) |

Other common keys: `Database__ConnectionString`, `Jwt__Issuer`, `Jwt__RefreshTokenDays`, `Jwt__SigningKeyPemBase64`, `Redis__ConnectionString`, `Email__FromAddress`, `Email__SessionToken`, `SecretProtection__KeyDirectoryPath`, etc.

In local development the `Bootstrap` section in `appsettings.Development.json` is sufficient.

> After bootstrap, remove `Bootstrap__*` from the production environment. They are only needed on the first run.

### Docker container image

Production image: [`Dockerfile`](./Dockerfile) → `mrffilipe/kyvo-api`. **Deploy:** [../GETTING_STARTED.md § Production](../GETTING_STARTED.md#7-production-deployment-docker-compose). **Build/push:** [../docs/DOCKER_PUBLISH.md](../docs/DOCKER_PUBLISH.md).

| Topic | Detail |
|-------|--------|
| Listen port | `8080` inside the container (`ASPNETCORE_URLS=http://+:8080`) |
| Migrations | EF Core migrations bundle runs when `Database__ApplyMigrationsOnStartup=true` (entrypoint) |
| JWT key (production) | `Jwt__SigningKeyPemBase64` — Base64-encoded PEM (no key file mount) |
| JWT key (local dev) | `Jwt__SigningKeyPath` — path to your PEM file (see below); not committed in appsettings |
| Data Protection | Mount volume at `/app/keys/data-protection`; set `SecretProtection__KeyDirectoryPath=keys/data-protection` |
| Health | Orchestrators can probe `GET /v1.0/platform/status` on port `8080` |
| HTTPS | Forwarded Headers enabled for TLS termination at an external reverse proxy |

### RSA key for OIDC

JWTs are signed with RSA (RS256). Generate a 2048-bit private key and configure exactly one signing source before starting the API.

**Linux / macOS:**

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out oidc-signing.pem
export Jwt__SigningKeyPath="$PWD/oidc-signing.pem"
```

**Windows** (OpenSSL on PATH):

```powershell
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out oidc-signing.pem
$env:Jwt__SigningKeyPath = (Resolve-Path oidc-signing.pem).Path
```

**Windows** (no OpenSSL — .NET):

```powershell
$path = Join-Path $env:LOCALAPPDATA 'kyvo\oidc-signing.pem'
New-Item -ItemType Directory -Force -Path (Split-Path $path) | Out-Null
$rsa = [System.Security.Cryptography.RSA]::Create(2048)
[System.IO.File]::WriteAllText($path, $rsa.ExportPkcs8PrivateKeyPem())
$env:Jwt__SigningKeyPath = $path
```

Configure `Jwt:SigningKeyPath` for local development, `Jwt:SigningKeyPem` for inline PEM, or `Jwt:SigningKeyPemBase64` / `Jwt__SigningKeyPemBase64` for production containers (encode with `openssl base64 -A -in oidc-signing.pem`). Use only one source at a time. Do not commit PEM files.

---

## Secret protection at rest

Identity provider configuration JSON (`IdentityProvider.ConfigJson`) frequently contains secrets (`clientSecret`, etc.). These sensitive top-level paths are encrypted before persistence using ASP.NET Core Data Protection through `ISecretProtector` and `IdentityProviderConfigCipher`.

- Plain-text payloads are still readable at runtime; they are re-encrypted lazily on the next write.
- Encrypted values are tagged with the prefix `enc:v1:`.
- The keyring is persisted under `SecretProtection:KeyDirectoryPath` (default `keys/data-protection`). **Lose those keys and previously stored secrets become unreadable** — back them up alongside the database.

`IdentityProviderDto` deliberately omits `ConfigJson`; secrets are never returned to API consumers.

---

## Build the Docker image

From the repository root:

```bash
docker build -f backend/Dockerfile -t mrffilipe/kyvo-api:<tag> .
```

---

## Run locally

```bash
cd backend

# 1. Restore dependencies
dotnet restore

# 2. Apply migrations (the database must already exist)
dotnet ef database update \
  --project Kyvo.Infrastructure \
  --startup-project Kyvo.API

# 3. Start the API
dotnet run --project Kyvo.API
```

The API runs at `http://localhost:5000`. Swagger is available under `/swagger` in Development/Staging.

---

## Bootstrap

The platform is initialized automatically on API startup, once, while `PlatformConfiguration.IsBootstrapped` is `false` and `Bootstrap:AdminEmail` / `Bootstrap:AdminPassword` are configured.

Configure credentials before starting the API (`Bootstrap` section in appsettings or `Bootstrap__*` env vars). On startup, the API creates:

- Root admin user (`ApplicationUser` + domain `User` profile) with ASP.NET Core Identity local credentials
- `plat_admin` platform role assigned to the admin
- `local` Identity Provider enabled
- Application `platform-admin` + Client `platform-admin-web` (fixed, not editable via API)
- A `PlatformConfiguration` row marking the system as bootstrapped

Check the status:

```bash
curl http://localhost:5000/v1.0/platform/status
# { "isConfigured": true, "requiresBootstrap": false, "oauthClientId": "platform-admin-web" }
```

> After a successful production initialization, remove `Bootstrap__*` from the environment. They no longer have any effect.

---

## Main endpoints

### Platform

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/v1.0/platform/status` | Public | Status and whether bootstrap is required |

### Account / OIDC

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/account/login` | Public | Login page (Blazor Web App Static SSR) |
| POST | `/account/signin` | Public | Local credential handler (cookie sign-in) |
| GET | `/account/register` | Public | Self-registration page (Blazor SSR) |
| POST | `/account/register` | Public, rate-limited | Self-registration handler (creates Identity user) |
| GET | `/login/federated/{alias}` | Public | Start federated OAuth redirect |
| GET/POST | `/callback/login/{alias}` | Public | Federated OAuth callback |
| POST | `/account/logout` | Cookie | End local session |
| GET/POST | `/connect/authorize` | Cookie | OIDC authorization endpoint |
| POST | `/connect/token` | Client credentials | Code-to-token exchange |
| GET/POST | `/connect/userinfo` | Bearer | OIDC userinfo |
| GET/POST | `/connect/logout` | Cookie / Bearer | OIDC logout |
| GET | `/.well-known/openid-configuration` | Public | OIDC discovery |
| GET | `/.well-known/jwks.json` | Public | Public RSA keys |

### Auth (JWT)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/v1.0/auth/subscribe` | JWT | SaaS onboarding (create a tenant via the OAuth app) |
| POST | `/v1.0/auth/switch-tenant` | JWT | Switch the active tenant in the session |
| GET | `/v1.0/auth/sessions` | JWT | List active sessions |
| DELETE | `/v1.0/auth/sessions/{id}` | JWT | Revoke a session |
| DELETE | `/v1.0/auth/account` | JWT + tenant context | Delete account for the current application tenant (owners hard-delete the tenant; non-owners revoke membership only) |

**Tenant metadata:** use `PATCH /v1.0/Tenants/{id}` to update the tenant name after `POST /v1.0/auth/subscribe` (`tenantKey` is immutable).

### Users

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/v1.0/Users` | JWT + tenant admin/owner or plat_admin | Search users by email or display name (picker) |
| GET | `/v1.0/Users/me` | JWT | Current user profile |
| PATCH | `/v1.0/Users/me` | JWT | Update profile |
| GET | `/v1.0/Users/me/memberships` | JWT | User memberships |

### Identity Providers

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/v1.0/IdentityProviders` | JWT + plat_admin | List IdPs |
| GET | `/v1.0/IdentityProviders/{id}` | JWT + plat_admin | Get IdP by id |
| GET | `/v1.0/IdentityProviders/aliases/{alias}/availability` | JWT + plat_admin | Check alias availability |
| POST | `/v1.0/IdentityProviders` | JWT + plat_admin | Add IdP (ConfigJson sensitive fields are encrypted on save) |
| PATCH | `/v1.0/IdentityProviders/{id}` | JWT + plat_admin | Update IdP |
| POST | `/v1.0/IdentityProviders/{id}/enable` | JWT + plat_admin | Enable |
| POST | `/v1.0/IdentityProviders/{id}/disable` | JWT + plat_admin | Disable |

### Applications (platform admin)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/v1.0/Applications` | JWT + plat_admin | List applications |
| GET | `/v1.0/Applications/slugs/{slug}/availability` | JWT + plat_admin | Check slug availability |
| GET | `/v1.0/Applications/{id}/branding` | JWT + plat_admin | Login branding settings |
| PATCH | `/v1.0/Applications/{id}/branding` | JWT + plat_admin | Update branding colors and hero copy |
| POST | `/v1.0/Applications/{id}/branding/logo` | JWT + plat_admin | Upload login logo |
| DELETE | `/v1.0/Applications/{id}/branding/logo` | JWT + plat_admin | Remove login logo |

### Audit logs

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/v1.0/AuditLogs` | JWT + tenant context | List audit logs (filtered) |
| GET | `/v1.0/AuditLogs/filter-options` | JWT + tenant context | Distinct actions/users for filters |

#### Federated identity providers

Each external IdP's configuration lives in the database (`ConfigJson`), entered through the admin console. All federated types share `FederatedProviderConfig` (`clientId`, `clientSecret`, optional `issuer` for GenericOidc).

| Type | `ConfigJson` | Login on `/account/login` |
|------|--------------|---------------------------|
| `Local` | optional / empty | email + password |
| `Google` | `clientId`, `clientSecret` | OAuth redirect via `/login/federated/{alias}` |
| `Microsoft` | `clientId`, `clientSecret` | OAuth redirect |
| `GitHub` | `clientId`, `clientSecret` | OAuth redirect |
| `GenericOidc` | `clientId`, `clientSecret`, `issuer` | OAuth redirect |

OIDC flow: the admin console kicks off `connect/authorize` → redirect to `/account/login` → methods displayed according to the **enabled** IdPs → session cookie → return to the client.

Federated sign-in uses OpenIddict Client: `/login/federated/{alias}` redirects to the upstream provider; `/callback/login/{alias}` completes the flow and sets the session cookie before continuing the OAuth `returnUrl`.

Register redirect URIs at the upstream provider as `https://<kyvo-host>/callback/login/<alias>`.

### Tenants (highlights)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/v1.0/Tenants/keys/{key}/availability` | JWT | Check tenant key availability |
| POST | `/v1.0/Tenants/{id}/invites` | JWT (owner/admin/plat_admin) | Send invite; persists only after SES succeeds; returns `id` + `acceptPath` |
| GET | `/v1.0/Tenants/{id}/invites` | JWT (owner/admin/plat_admin) | List tenant invites (`acceptPath` on pending invites with encrypted token) |
| DELETE | `/v1.0/Invites/{id}` | JWT (owner/admin/plat_admin) | Revoke a pending invite |
| POST | `/v1.0/invites/accept` | JWT | Accept invite by token |

Invite tokens are stored hashed (`token_hash`) for validation and encrypted at rest (`encrypted_token` via Data Protection) so admins can copy pending invite links. Legacy invites without `encrypted_token` list with `acceptPath: null`.

Memberships and additional application CRUD: see `frontend/swagger.json`.

---

## Authorization

- **Claim `prole=plat_admin`**: platform administrator. Resolved by reading `UserPlatformRole` + `PlatformRole` from the database.
- **Policy `PlatformAdministrator`**: protects tenant/application/IdP management.
- **`trole`**: roles of the active tenant (owner, admin, member, viewer).
- **Tenant context**: `tid` (tenant id) and `mid` (membership id) claims in the JWT.

---

## Domain entities

| Entity | Table | Description |
|--------|-------|-------------|
| `User` | `users` | Platform user (links to `AspNetUsers`) |
| `UserPlatformRole` | `user_platform_roles` | Platform role assignment |
| `PlatformRole` | `platform_roles` | Global roles (e.g., `plat_admin`) |
| `IdentityProvider` | `identity_providers` | IdP configuration (Local, Google, Microsoft, GitHub, GenericOidc) |
| `Tenant` | `tenants` | Organization / isolated space |
| `TenantRole` | `tenant_roles` | Per-tenant configurable roles |
| `TenantMembership` | `tenant_memberships` | User ↔ tenant link |
| `Application` | `applications` | Registered OAuth application |
| `ApplicationClient` | `application_clients` | OAuth client (public/confidential) |
| `ApplicationTenant` | `application_tenants` | App ↔ tenant link (provisioning) |
| `AuthSession` | `auth_sessions` | Active session (binds cookie to JWT) |
| `AuditLog` | `audit_logs` | Per-tenant event log |
| `TenantInvite` | `tenant_invites` | Tenant invite |

---

## Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName \
  --project Kyvo.Infrastructure \
  --startup-project Kyvo.API \
  --output-dir Migrations

# Apply to the database
dotnet ef database update \
  --project Kyvo.Infrastructure \
  --startup-project Kyvo.API

# Remove the last (unapplied) migration
dotnet ef migrations remove \
  --project Kyvo.Infrastructure \
  --startup-project Kyvo.API
```

---

## Project structure

```
Kyvo.API/
├── Components/          Blazor Web App (Static SSR) - login/register pages and shared layout
│   ├── App.razor            Root document (html/head/body)
│   ├── Routes.razor         Router
│   ├── Layout/              AccountLayout (split-screen with hero)
│   └── Pages/Account/       Login.razor, Register.razor
├── Controllers/         API controllers (Account POST handlers, Authorization, v1 REST)
├── Common/              Base controllers, middlewares, OIDC helpers
├── Services/            API-only services (e.g. IFederatedConfigBuilder for login UI)
├── Swagger/             OpenAPI filters and registration
├── wwwroot/css/         Modern, theme-aware account.css (no framework, no build step)
├── appsettings.json     Configuration template
└── Program.cs           Startup (DI, OIDC, policies, rate limiting, Razor components)

Kyvo.Application/
├── UseCases/            I*UseCase interfaces per workflow (Auth, Tenants, Applications, …)
├── Queries/             I*Query interfaces for reads
├── Policies/            ITenantAuthorizationPolicy, ITenantAccountEligibilityPolicy
├── Shared/              ITenantProvisioner, TenantContextBuilder
├── Ports/               IUserAccountService, IEmailService, IApplicationBrandingStorage, IKyvoClaimsPrincipalFactory, IPlatformBootstrapExecutor
├── Services/            Legacy helpers (e.g. IdentityProvider config validation)
├── Common/              PagedRequest, PagedResult, ApplicationClientListFields
└── Exceptions/          ApplicationErrorMessages (static messages)

Kyvo.Infrastructure/
├── Configurations/      *Options, InfrastructureErrorMessages, IValidateOptions validators
├── Extensions/          AddInfrastructure, AddUseCases, AddQueries, AddRepositories, AddServices
├── Identity/            ApplicationUser, UserMapper, UserAccountService
├── Platform/            PlatformBootstrapExecutor (serializable bootstrap transaction)
├── UseCases/            Use case implementations
├── Queries/             Query implementations
├── Policies/            Policy implementations
├── Shared/              TenantProvisioner
├── Migrations/          InitialCreate (squashed baseline)
├── Persistence/
│   ├── Configurations/  EF Fluent API per entity
│   ├── Repositories/    Repository implementations
│   └── ApplicationDbContext.cs
└── Services/            OpenIddict sync, claims principal factory, email (AWS SES), security
```
