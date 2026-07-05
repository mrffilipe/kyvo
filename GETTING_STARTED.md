# Getting Started — Kyvo

[English](./GETTING_STARTED.md) | [Português](./GETTING_STARTED.pt-BR.md)

> **Pronunciation:** *Kyvo* is pronounced like **"Key"vo** — rhymes with English *key* plus *vo*.

Guide to run Kyvo in **development** (Docker Compose + `.env` from this repo) or **production** (published Docker images).

### Choose your path

| Path | Audience | Sections |
|------|----------|----------|
| **Development** | You cloned this repository and run the API and admin SPA with Docker Compose + `.env` | **1–6** below |
| **Production** | You deploy published images with Docker Compose (no build from this repo) | **[§ 7 — Production deployment](#7-production-deployment-docker-compose)** |

> **Maintainers** (build and push images): see [docs/DOCKER_PUBLISH.md](./docs/DOCKER_PUBLISH.md), not this guide.

---

## Development (sections 1–6)

---

## 1. Prerequisites

Install before continuing:

| Tool | How to install | Minimum version | Purpose |
|------|----------------|-----------------|---------|
| Docker Engine + Compose v2 | [docker.com](https://docs.docker.com/get-docker/) | Current | Run API and admin SPA containers |
| PostgreSQL | [postgresql.org](https://www.postgresql.org/download/) | 14 | Database on the **host** (not in Kyvo compose) |
| Redis | [redis.io](https://redis.io/downloads/) | Optional | Cache on the host; API falls back to in-memory if unset |
| .NET SDK | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0 | Run `dotnet ef` migrations from the host |
| dotnet-ef (CLI) | `dotnet tool install --global dotnet-ef` | 8.x | Apply EF migrations |
| openssl | Bundled with macOS/Linux; Windows: Git for Windows or `winget install ShiningLight.OpenSSL` | Any | Generate the OIDC RSA signing key |

Clone the repository:

```bash
git clone https://github.com/mrffilipe/kyvo.git
cd kyvo
```

---

## 2. Configure the database

Create a PostgreSQL database on your machine (or any server you manage). The API container reaches it via **`host.docker.internal`** (see [backend/.env.example](./backend/.env.example)).

```sql
CREATE DATABASE kyvo_db;
```

Or via the command line:

```bash
createdb kyvo_db
```

PostgreSQL and Redis are **not** included in [backend/docker-compose.yml](./backend/docker-compose.yml). Run them on the host (or elsewhere) and point `Database__ConnectionString` / `Redis__ConnectionString` in `backend/.env` at `host.docker.internal`.

| Who connects | Database host in connection string | Why |
|--------------|-----------------------------------|-----|
| API container (`backend/.env`) | `host.docker.internal` | Docker DNS name for services on the host |
| `dotnet ef` on the host | `localhost` | CLI runs outside the container |

---

## 3. Configure the backend

Development uses [backend/docker-compose.yml](./backend/docker-compose.yml) and [backend/.env.example](./backend/.env.example). Configuration is split between **`.env`** (API container) and **`appsettings.Development.json`** (`dotnet ef` on the host):

| File | Used by | `Database` host |
|------|---------|-----------------|
| `backend/.env` | `docker compose` (API container) | `host.docker.internal` |
| `Kyvo.API/appsettings.Development.json` | `dotnet ef` (host) | `localhost` |

Keep credentials and other values aligned between both files (see `.env.example` comments).

### 3.1 Prepare the `.env` file

```bash
cd backend
cp .env.example .env
```

Edit `.env` with your PostgreSQL/Redis credentials, bootstrap admin, and compose settings (`API_PORT`). The template already uses `host.docker.internal` for database and Redis.

### 3.2 OIDC signing key (RSA)

Kyvo signs OIDC tokens with **RS256** (RSA + SHA-256). Configure **exactly one** signing key source. Never commit the PEM file (`backend/keys/*.pem` is gitignored).

| Scenario | Variable | How to supply the key |
|----------|----------|------------------------|
| **Development** (compose) | `Jwt__SigningKeyPath=keys/oidc-signing.pem` | Generate PEM at `backend/keys/oidc-signing.pem` **before** the first `docker compose up`; compose bind-mounts `./keys` |
| **Production** (§7) | `Jwt__SigningKeyPemBase64` | Generate PEM off-repo → Base64-encode → paste in deploy `.env`; **do not** mount a key file |
| Avoid | Multiple sources | Set only **one** of Path / Pem / PemBase64 |

**Generate the PEM** (2048-bit RSA private key) **before** starting Docker Compose. If the file is missing, Docker Desktop may create `keys/oidc-signing.pem` as a **directory** and the container will fail to start — delete that folder and generate a real PEM file.

```bash
cd backend
mkdir -p keys
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out keys/oidc-signing.pem
```

**Windows** (OpenSSL on PATH):

```powershell
cd backend
New-Item -ItemType Directory -Force -Path keys | Out-Null
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out keys/oidc-signing.pem
```

**Windows** (no OpenSSL — Docker):

```powershell
cd backend
New-Item -ItemType Directory -Force -Path keys | Out-Null
docker run --rm -v "${PWD}/keys:/keys" alpine/openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out /keys/oidc-signing.pem
```

**Windows** (no OpenSSL — .NET 5+ / PowerShell 7+):

```powershell
cd backend
New-Item -ItemType Directory -Force -Path keys | Out-Null
$rsa = [System.Security.Cryptography.RSA]::Create(2048)
[System.IO.File]::WriteAllText("$PWD\keys\oidc-signing.pem", $rsa.ExportPkcs8PrivateKeyPem())
```

**Development wiring** (already in `.env.example`):

```env
Jwt__SigningKeyPath=keys/oidc-signing.pem
Jwt__SigningKeyPem=
Jwt__SigningKeyPemBase64=
Jwt__Issuer=http://localhost:5000
```

- `Jwt__Issuer` must match the URL **browsers** use to reach the API (`http://localhost:5000` with default `API_PORT=5000`).
- Inside the container the key path is `keys/oidc-signing.pem`; on the host it is `backend/keys/oidc-signing.pem`.

**Production:** encode the same PEM as Base64 (see [§7](#7-production-deployment-docker-compose)); set `Jwt__SigningKeyPemBase64` and leave `Jwt__SigningKeyPath` empty.

### 3.3 Bootstrap admin credentials

Set the first administrator in `backend/.env`:

```env
Bootstrap__AdminEmail=admin@localhost
Bootstrap__AdminPassword=YourSecurePassword@123
Bootstrap__AdminDisplayName=Admin
```

> Never commit real passwords. After the first successful login in production, remove `Bootstrap__*` from the environment (see §7).

### 3.4 Apply migrations (on the host)

The dev container image does not run the EF migrations bundle. Apply migrations **from your machine**.

1. Set `Database:ConnectionString` in `Kyvo.API/appsettings.Development.json` to use **`localhost`** (not `host.docker.internal`). Adjust username, password, and database name to match your PostgreSQL install.
2. Run:

```bash
cd backend

dotnet ef database update \
  --project Kyvo.Infrastructure \
  --startup-project Kyvo.API
```

`dotnet ef` loads configuration through `ApplicationDbContextFactory`, which reads **`appsettings.Development.json`** (and optional `Database__ConnectionString` from the environment). It does **not** read `backend/.env`.

Optional one-off override:

```bash
# Linux / macOS
export Database__ConnectionString="Host=localhost;Port=5432;Database=kyvo_db;Username=postgres;Password=postgrespassword"
```

```powershell
# Windows (PowerShell)
$env:Database__ConnectionString = "Host=localhost;Port=5432;Database=kyvo_db;Username=postgres;Password=postgrespassword"
```

This creates every table (`AspNetUsers`, `OpenIddict*` entities, `identity_providers`, `tenants`, `applications`, `application_clients`, `auth_sessions`, `audit_logs`, etc.).

### 3.5 Start the API

```bash
cd backend
docker compose up -d --build
```

The API is available at `http://localhost:5000` (default `API_PORT`). Swagger: `http://localhost:5000/swagger`.

Confirm it is healthy:

```bash
curl http://localhost:5000/v1.0/platform/status
# Expected: { "isConfigured": true, "requiresBootstrap": false, "oauthClientId": "platform-admin-web" }
```

On first startup, the API initializes the platform (admin, local IdP, OAuth client) using `Bootstrap__*` from `.env`. If those variables are missing, status stays `requiresBootstrap: true` until you configure `.env` and restart:

```bash
docker compose restart kyvo.api
```

---

## 4. Configure and start the frontend

Start the **backend** (§3.5) before the frontend.

### 4.1 Prepare the `.env` file

```bash
cd frontend
cp .env.example .env
```

[frontend/.env.example](./frontend/.env.example) documents the variables used by [frontend/docker-compose.yml](./frontend/docker-compose.yml):

```env
FRONTEND_PORT=3000
VITE_API_BASE_URL=http://localhost:5000
VITE_API_VERSION=1.0
VITE_API_TIMEOUT_MS=30000
VITE_OAUTH_CLIENT_ID=platform-admin-web
VITE_OAUTH_REDIRECT_URI=http://localhost:3000/auth/callback
```

Defaults match the API on port `5000` and the SPA on port `3000`. Change them only if you use different ports.

### 4.2 Start the SPA

```bash
cd frontend
docker compose up
```

The admin SPA runs at `http://localhost:3000`.

---

## 5. Sign in

Open `http://localhost:3000` (API and frontend containers running).

The platform is initialized automatically when the API starts (with `Bootstrap__*` in `backend/.env`). On the first successful run, the API creates:

- Admin user with the password from `Bootstrap__AdminPassword`
- Platform role `plat_admin` assigned to the admin
- `local` Identity Provider enabled
- Application `platform-admin` + OAuth Client `platform-admin-web` (fixed, not editable via API)

If `Bootstrap__*` is not configured, `/login` shows a message asking you to configure `backend/.env` and restart the API.

Check the status:

```bash
curl http://localhost:5000/v1.0/platform/status
# { "isConfigured": true, "requiresBootstrap": false, "oauthClientId": "platform-admin-web" }
```

> After a successful production initialization, remove `Bootstrap__*` from the deploy `.env`. They no longer have any effect.

### Sign in

1. Click **"Sign in to the platform"**
2. You are redirected to `/account/login` on the backend (Blazor SSR page; federated providers redirect via `/login/federated/{alias}`)
3. Enter the email and password from `Bootstrap__*` in `backend/.env` (e.g., `admin@localhost` / `YourSecurePassword@123`)
4. After authentication, the backend redirects to the OIDC callback
5. The frontend stores the tokens and opens the admin console

### Self-registration (new users)

For end users who do NOT yet have an account in the platform (typical SaaS onboarding):

1. From any consumer app (e.g., Pulse CRM) the user clicks "Sign in" and is redirected to `/connect/authorize`.
2. The IdP login page exposes a **Create account** link to `/account/register`.
3. The user fills email, password (matching `PasswordPolicy` requirements) and display name. The endpoint is rate-limited by the `account_register` policy.
4. After successful registration the platform creates an Identity user and signs the user in via the cookie scheme — NO tenant or membership is created at this point.
5. The user is redirected back to `/connect/authorize`; the consumer app receives the OIDC `code`.
6. The consumer app detects the missing `tid` claim in the access token and triggers its onboarding flow, calling `POST /v1.0/auth/subscribe` with tenant + plan to attach the user to a tenant. After a refresh token, the new access token includes `tid` / `mid`.
7. To update tenant metadata later, use `PATCH /v1.0/Tenants/{id}` (name only; `tenantKey` is immutable). To leave an application, call `DELETE /v1.0/auth/account` in the OAuth session context — owners hard-delete the tenant when there are no blocking issues; the global user record is removed only when no active memberships remain.

This central signup model means client apps NEVER implement their own "create account" pages; password collection only happens on the IdP domain.

---

## 6. Next steps

### Create a tenant

In the admin console go to **Tenants** → **Create tenant**. Provide a name and a unique key (e.g., `my-org`).

### Invite members

Inside a tenant, navigate to **Tenants** → **Invite member** (or open **Memberships** to manage pending invites). AWS SES must be configured under `Email.*` for delivery — if email sending fails, **no invite is persisted**. On success, the API returns `acceptPath` (e.g. `/accept-invite?token=…`); copy the full URL from the admin console or from **Memberships** → pending invites.

### Register an OAuth application

Go to **Applications** → **New application**. After creation, open the details and register an **OAuth Client** with your consumer application's redirect URIs.

### Add external identity providers (optional)

As a platform admin, navigate to **Identity Providers** → **Add IdP**. The `local` provider (bootstrap) stays enabled for email/password.

Identity provider OAuth client secrets (`clientSecret` in `ConfigJson`) are stored **encrypted at rest** using ASP.NET Core Data Protection. The plaintext values are required during creation/update only and are never returned by `GET` endpoints.

#### Capabilities

Each identity provider declares one or more `IdpCapability` flags. The admin form surfaces them as checkboxes:

| Capability | Allowed for | Conflict policy |
|------------|-------------|-----------------|
| `LocalPassword` | `Local` only (hard-locked) | Only **one** enabled provider can advertise it. Adding a second one fails. |
| `GoogleSocial` | Google, GenericOidc | Adding a second enabled provider returns a `warnings` payload but is allowed. |
| `MicrosoftSocial` | Microsoft, GenericOidc | Soft warning on conflict. |
| `AppleSocial` | GenericOidc | Soft warning on conflict. |
| `GenericOidc` | GenericOidc | Soft warning on conflict. |

The hard-lock for `LocalPassword` mirrors what Microsoft Entra and other enterprise IdPs do: a single source of email/password authentication keeps account linking deterministic and avoids UI ambiguity ("which email/password form is legitimate?"). Social providers are softer: legitimate multi-realm setups can run two Google connections side by side and you only get a warning so the admin acknowledges the conflict.

#### Federated OAuth redirect (Google, Microsoft, GitHub, Generic OIDC)

External providers use **OAuth redirect** (OpenIddict Client), not popup or id_token POST. Configuration is a single `FederatedProviderConfig` schema in `ConfigJson`:

| Field | Required | Purpose |
|-------|----------|---------|
| `clientId` | Yes | OAuth client id from the upstream provider |
| `clientSecret` | Yes | OAuth client secret (encrypted at rest) |
| `issuer` | GenericOidc only | Issuer URL for discovery (well-known providers resolve issuer from preset) |

Steps:

1. Create an OAuth app in Google Cloud / Azure / GitHub (or your OIDC issuer) with redirect URI `https://<kyvo-host>/callback/login/<alias>`.
2. Admin console (`http://localhost:3000`) → **Identity Providers** → **Add IdP** → type **Google** (or Microsoft/GitHub/GenericOidc), alias e.g. `google`, enter `clientId` + `clientSecret` → **Enabled**.
3. Keep the `local` IdP enabled (from bootstrap).
4. Test: any OIDC app → redirect → `/account/login` → **Continue with Google** (or provider button) → upstream OAuth → `/callback/login/google` → session cookie → OAuth `returnUrl`.

**Pulse CRM with Google:** the CRM does not integrate Google directly; it redirects to the platform OIDC. With a Google IdP enabled, on `/account/login` the user signs in via redirect, returns to the CRM with a `code`, completes onboarding/subscribe, refreshes tokens for `tid`/`mid` claims, and uses the API normally. See `samples/pulse-crm/backend/README.md`.

### Integrate a consumer application

1. Register an **Application** and an **OAuth Client** in the admin console (your app's redirect URIs).
2. Use the discovery URL: `http://localhost:5000/.well-known/openid-configuration` (in production replace it with the public host of the API).
3. Implement authorization code + PKCE in your client (SPA, backend, etc.).

---

## 7. Production deployment (Docker Compose)

Deploy Kyvo using **two published container images** (API + admin SPA). TLS termination and path routing are handled by an **external reverse proxy** (Traefik on Coolify, nginx, etc.). You do not need to clone this repository except optionally to generate the OIDC signing key (see [§3.2](#32-oidc-signing-key-rsa)).

**PostgreSQL and Redis are required** and are not included in the application compose example below.

### Development vs production

| | Development (this repo) | Production (`kyvo-deploy/`) |
|---|-------------------------|------------------------------|
| Compose file | [backend/docker-compose.yml](./backend/docker-compose.yml) | `docker-compose.yml` in your deploy folder (snippet below) |
| API image | Built locally (`Kyvo.API/Dockerfile`) | `mrffilipe/kyvo-api:${IMAGE_TAG}` |
| Frontend | [frontend/docker-compose.yml](./frontend/docker-compose.yml) (Vite dev) | `mrffilipe/kyvo-frontend:${IMAGE_TAG}` |
| JWT signing key | `Jwt__SigningKeyPath` + PEM volume mount | `Jwt__SigningKeyPemBase64` only (no PEM mount) |
| Migrations | `dotnet ef` on the host (§3.4) | `Database__ApplyMigrationsOnStartup=true` (production image entrypoint) |
| Config | `backend/.env` from [backend/.env.example](./backend/.env.example) | `.env` next to deploy `docker-compose.yml` |

### Prerequisites

| Tool | Purpose |
|------|---------|
| Docker Engine + Docker Compose v2 | Run containers |
| PostgreSQL + Redis | Reachable from the API container (`host.docker.internal` or managed hostnames) |
| Published images on Docker Hub | `mrffilipe/kyvo-api:<tag>` and `mrffilipe/kyvo-frontend:<tag>` (set `IMAGE_TAG` in `.env`) |
| Reverse proxy with HTTPS | Routes one public host to API paths and SPA paths (see below) |

You do **not** need the .NET SDK or Node.js on the host unless you generate the OIDC key from this repo.

### Single public URL (recommended)

With `Jwt__Issuer=https://auth.example.com` and TLS on that host, users and the SPA use the **same origin**. The proxy forwards API paths to `kyvo-api` and everything else to `kyvo-frontend`:

| What you open or call | URL | Routed to |
|----------------------|-----|-----------|
| Admin console (SPA) | `https://auth.example.com/` | `kyvo-frontend` (nginx :80) |
| OAuth callback | `https://auth.example.com/auth/callback` | `kyvo-frontend` |
| API (JSON, OIDC, login pages) | `https://auth.example.com/v1.0/...`, `/connect/...`, `/account/...`, `/.well-known/...`, `/swagger`, `/css/...`, `/js/...` | `kyvo-api` (:8080) |

Set **`Jwt__Issuer`** to exactly the URL browsers use (scheme + host, no trailing slash). The frontend image is built with empty `VITE_*` URLs so the SPA uses `window.location.origin` at runtime.

**Proxy path prefixes for the API** (must not be served by the SPA container):

- `/v1.0/`
- `/connect/`
- `/account/`
- `/.well-known/`
- `/swagger`
- `/css/`, `/js/`, and `/brand/` (Blazor account static assets — e.g. `account-theme.js`, `firebase-google-signin.js`, logos)

### Suggested deploy directory

Create a folder outside this repository (for example `kyvo-deploy/`) with:

```
kyvo-deploy/
  docker-compose.yml
  .env
```

### PostgreSQL and Redis (infra)

Save as `docker-compose.infra.yml` in the same deploy folder (or use managed services).

```yaml
# Suggested local PostgreSQL + Redis (not part of the Kyvo repo)
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

Example `.env` for the snippet (same directory as the file above):

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgrespassword
POSTGRES_DB=kyvo_db
POSTGRES_PORT=5432
REDIS_PASSWORD=default_password
REDIS_PORT=6379
```

Start infra:

```bash
docker compose -f docker-compose.infra.yml --env-file .env.infra up -d
```

| Infra variable | Suggested default | Purpose |
|----------------|-------------------|---------|
| `POSTGRES_USER` | `postgres` | Database user |
| `POSTGRES_PASSWORD` | (set a strong value) | Database password |
| `POSTGRES_DB` | `kyvo_db` | Database name |
| `POSTGRES_PORT` | `5432` | Published host port |
| `REDIS_PASSWORD` | (set a strong value) | Redis password |
| `REDIS_PORT` | `6379` | Published host port |

Align `Database__ConnectionString` and `Redis__ConnectionString` in `.env` with these values (the example below uses `host.docker.internal` when infra publishes ports on the host).

### `docker-compose.yml` (production)

Save as `docker-compose.yml` in your deploy directory. Expose ports only if your proxy runs on the same host; on Coolify/Traefik you typically attach labels instead of publishing ports.

```yaml
# Kyvo — split images (API + admin SPA). TLS at external proxy.
# Requires PostgreSQL and Redis reachable from the API container.

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

### `.env` (application)

Save as `.env` next to `docker-compose.yml`:

```env
# --- Published images (Docker Hub) ---
IMAGE_TAG=2.0.0
API_PORT=8080
FRONTEND_PORT=8081

# --- Database (required) ---
Database__ConnectionString=Host=host.docker.internal;Port=5432;Database=kyvo_db;Username=postgres;Password=postgrespassword
Database__ApplyMigrationsOnStartup=true

# --- JWT / OIDC (required) ---
# Must match the public URL users use (same host as the external HTTPS proxy).
Jwt__Issuer=https://auth.example.com
Jwt__Audience=kyvo-api
Jwt__KeyId=default
Jwt__RefreshTokenDays=30
# Production: RSA private key as Base64 only — see "OIDC signing key (production)" below.
Jwt__SigningKeyPath=
Jwt__SigningKeyPem=
Jwt__SigningKeyPemBase64=

# --- Redis (recommended) ---
Redis__ConnectionString=host.docker.internal:6379,password=default_password,ssl=false
Redis__InstanceName=kyvo:
Redis__TenantIdentifierCacheMinutes=5

# --- Data Protection ---
SecretProtection__KeyDirectoryPath=keys/data-protection
SecretProtection__ApplicationName=Kyvo

# --- Bootstrap (first deploy only — remove after success) ---
Bootstrap__AdminEmail=admin@example.com
Bootstrap__AdminPassword=ChangeMe_Strong_Password_12
Bootstrap__AdminDisplayName=Admin

# --- Email (AWS SES — for invites) ---
Email__FromAddress=noreply@example.com
Email__Region=us-east-1
Email__AccessKeyId=
Email__SecretAccessKey=
Email__SessionToken=
```

### OIDC signing key (production)

1. **Generate** the PEM (same commands as [§3.2](#32-oidc-signing-key-rsa)) on a trusted machine. Store `oidc-signing.pem` securely; do not commit it or bake it into an image.
2. **Encode** the PEM as a single-line Base64 string:

```bash
# Linux/macOS
openssl base64 -A -in oidc-signing.pem

# PowerShell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("oidc-signing.pem"))
```

3. **Set** in deploy `.env`:

```env
Jwt__SigningKeyPemBase64=<paste-base64-here>
Jwt__SigningKeyPath=
Jwt__SigningKeyPem=
```

Use **only** `Jwt__SigningKeyPemBase64` in production. Leave `Jwt__SigningKeyPath` and `Jwt__SigningKeyPem` empty to avoid startup errors.

ASP.NET Core uses the `Section__Property` form. You do **not** set `VITE_*` in `.env` for production — the frontend image uses **same-origin** routing when built without custom build-args.

| Variable | Rebuild image? | Notes |
|----------|----------------|-------|
| `Database__*`, `Redis__*`, `Jwt__*`, `Bootstrap__*`, `Email__*` | No | Edit `.env`, then `docker compose restart api` |
| `Jwt__Issuer` | No | Must match your public URL (`https://auth.example.com`) |
| Platform code | Yes | Pull new `kyvo-api` and `kyvo-frontend` tags with the same `IMAGE_TAG` |

For **separate API/UI hosts**, rebuild `kyvo-frontend` with `--build-arg VITE_API_BASE_URL=...` and `VITE_OAUTH_REDIRECT_URI=...` (see [frontend/Dockerfile](./frontend/Dockerfile)).

### Deploy steps

1. Start PostgreSQL and Redis (infra snippet or managed services).
2. Generate `oidc-signing.pem` ([§3.2](#32-oidc-signing-key-rsa)).
3. Base64-encode the PEM and set `Jwt__SigningKeyPemBase64` in `.env` (leave `Jwt__SigningKeyPath` empty).
4. Create the deploy directory files above; configure your reverse proxy (HTTPS + path routing).
5. Set `Jwt__Issuer` to your public `https://` URL (same host users will open in the browser).
6. Start the stack:

```bash
cd kyvo-deploy
docker compose --env-file .env up -d
```

7. Open `https://your-public-host` (the API initializes automatically on startup). After confirming login works, remove `Bootstrap__*` from `.env` and restart:

```bash
docker compose --env-file .env restart api
```

### Production troubleshooting

| Issue | Solution |
|-------|----------|
| Cannot connect to database | Verify PostgreSQL and `Database__ConnectionString` |
| API container exits or unhealthy | `docker logs kyvo-api` — often missing or invalid `Jwt__SigningKeyPemBase64` |
| API restart: "Configure only one of Jwt:SigningKeyPath…" | `Jwt__SigningKeyPemBase64` is set but `SigningKeyPath` still comes from defaults | Set only `Jwt__SigningKeyPemBase64`; clear path with `Jwt__SigningKeyPath=` (empty). Images after 1.0.1 ship with empty path in `appsettings.json`. |
| OAuth redirect mismatch | Align `Jwt__Issuer` with the browser URL; verify OAuth client redirect URI (`https://<host>/auth/callback`) |
| SPA calls wrong API URL | Same-origin: set `Jwt__Issuer` to the browser URL; split hosts: rebuild frontend with `VITE_*` build-args |
| 404 on `/connect` or `/account` | Proxy must route API path prefixes to `kyvo-api`, not `kyvo-frontend` |
| 404 on `firebase-google-signin.js` or `account-theme.js` | `/js/` not routed to API (only `/css/` configured) | Add `PathPrefix(\`/js\`)` to the API Traefik rule |

---

## 8. Production configuration

### Critical environment variables

| Environment variable (`__`) | Production |
|-----------------------------|------------|
| `Database__ConnectionString` | Managed database connection string (RDS, Cloud SQL, etc.) |
| `Jwt__SigningKeyPemBase64` | Base64-encoded PEM of the RSA private key (production; no key file mount) |
| `Jwt__Issuer` | Public backend URL (e.g., `https://auth.mysite.com`) |
| `Bootstrap__AdminEmail` | Only on the first deploy; remove after bootstrap |
| `Bootstrap__AdminPassword` | Only on the first deploy; remove after bootstrap |
| `Bootstrap__AdminDisplayName` | Optional on the first deploy |
| `Email__FromAddress`, `Email__Region`, etc. | AWS SES configuration for invites |
| `Redis__ConnectionString` | Distributed cache (ElastiCache, Redis Cloud, etc.) |
| `SecretProtection__KeyDirectoryPath` | Persistent directory for the data protection key ring (must survive restarts and be backed up) |
| `SecretProtection__ApplicationName` | Logical name for key isolation (defaults to `Kyvo`) |
In a production `appsettings.json`, use `:` instead (e.g., `Database:ConnectionString`).

### Frontend in production

The admin SPA runs in `mrffilipe/kyvo-frontend` (nginx on port 80, HTTP only). Configure `Jwt__Issuer` in `.env` (section 7) and route the public host through your proxy. For API and UI on different hosts, rebuild the frontend image with `VITE_*` build-args.

### HTTPS

In production every connection must use HTTPS. `Jwt:Issuer` must use `https://` for OIDC to work correctly.

---

## 9. Command quick reference

```bash
# Backend: prepare env and start API (development)
cd backend && cp .env.example .env
cd backend && docker compose up -d --build
cd backend && docker compose logs -f kyvo.api
cd backend && docker compose restart kyvo.api

# Backend: apply migrations (reads appsettings.Development.json — use localhost for Database host)
cd backend
dotnet ef database update --project Kyvo.Infrastructure --startup-project Kyvo.API

# Backend: create a new migration
dotnet ef migrations add MigrationName --project Kyvo.Infrastructure --startup-project Kyvo.API --output-dir Migrations

# Frontend: prepare env and start SPA (development)
cd frontend && cp .env.example .env
cd frontend && docker compose up

# OIDC signing key (see §3.2)
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out backend/keys/oidc-signing.pem

# Platform status (after starting the API)
curl http://localhost:5000/v1.0/platform/status
```

---

## 10. Troubleshooting

| Issue | Likely cause | Solution |
|-------|--------------|----------|
| API fails to start: RSA key error | No signing key or multiple sources configured | Generate PEM (§3.2); dev: `Jwt__SigningKeyPath` + volume; prod: `Jwt__SigningKeyPemBase64` only |
| Compose: `not a directory` mounting `oidc-signing.pem` | PEM missing before first `up` — Docker created a folder with that name | Remove `backend/keys/oidc-signing.pem` if it is a directory; generate a PEM file (§3.2), then retry |
| API restart: "Configure only one of Jwt:SigningKeyPath…" | Both Base64 and Path set | Production: clear `Jwt__SigningKeyPath` and `Jwt__SigningKeyPem`; use only `Jwt__SigningKeyPemBase64` |
| `dotnet ef` cannot connect | Wrong host or credentials in `appsettings.Development.json` | Use `Host=localhost` for migrations; keep `.env` on `host.docker.internal` for the container. `dotnet ef` does not read `backend/.env` |
| `dotnet ef` password error despite correct `.env` | EF uses `ApplicationDbContextFactory` + appsettings, not `.env` | Set `Database:ConnectionString` in `appsettings.Development.json` (or export `Database__ConnectionString`) |
| API container cannot reach PostgreSQL | Wrong host in `backend/.env` | Use `host.docker.internal` in `Database__ConnectionString` when DB runs on the host |
| Platform not initialized (`requiresBootstrap: true`) | `Bootstrap__*` missing in `backend/.env` | Set `Bootstrap__AdminEmail` / `Bootstrap__AdminPassword` and `docker compose restart kyvo.api` |
| Frontend does not load after login | `VITE_OAUTH_REDIRECT_URI` is wrong | Confirm `redirect_uri` matches `platform-admin-web` in `frontend/.env` |
| Expired JWT / 401 | Token expired and refresh failed | Sign out and sign in again |
| Invites do not arrive by email | AWS SES is not configured | Configure `Email__*` with valid SES credentials in `.env` |
| CORS error | Frontend on a different URL | Verify `VITE_API_BASE_URL` in `frontend/.env` |
| Cannot decrypt an existing IdP configuration | Data Protection key ring lost | Restore `SecretProtection__KeyDirectoryPath` from backup, or recreate the IdP entry |
| Docker: cannot connect to PostgreSQL/Redis | Infra not running or wrong connection strings | Start infra or managed services; check `Database__*` and `Redis__*` in deploy `.env` |
| Docker: OAuth redirect error after login | `Jwt__Issuer` does not match the browser URL | Set `Jwt__Issuer` to your public URL and restart `api` |
| Docker: HTTPS / OIDC scheme wrong | Proxy not forwarding `X-Forwarded-Proto` or wrong `Jwt__Issuer` | Terminate TLS at the proxy; set `Jwt__Issuer` to `https://...` |
