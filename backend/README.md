# Kyvo Backend (.NET 8)

Identity Provider built with **ASP.NET Core Identity + OpenIddict**, pure OIDC at `/connect/*`, and tenant context via isolated `/api/v1/auth/switch-tenant`.

## Architecture (OIDC vs Kyvo API)

```
OIDC pure (/connect/*)          Kyvo API (/api/v1/*)
─────────────────────          ────────────────────
authorize → token              switch-tenant / subscribe → tenant JWT
Platform JWT: sub, email, sid  Tenant JWT: tid, mid, trole, token_use=tenant
```

**Rule:** `/connect/*` never injects `tid`/`mid`/`trole`. Tenant JWT is issued only by `POST /api/v1/auth/switch-tenant` or `POST /api/v1/auth/subscribe`.

## Two-step auth (SDK)

```typescript
await kyvo.oidc.signInRedirect()
// callback → platform token saved

const ctx = await kyvo.switchTenant(tenantId)  // uses platform token
kyvo.getAccessToken()                           // returns tenant JWT
```

## Run locally

```bash
cd backend
dotnet ef database update --project Kyvo.Infrastructure --startup-project Kyvo.API
dotnet run --project Kyvo.API
```

See [GETTING_STARTED.md](../GETTING_STARTED.md) for Docker Compose, `.env`, and OIDC signing keys.

## Main endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /account/login` | Local login + federated IdP buttons |
| `GET /login/federated/{alias}` | Upstream OIDC challenge |
| `GET /connect/authorize` | Authorization Code + PKCE |
| `POST /connect/token` | Code/refresh exchange (rate limited 5/min) |
| `POST /api/v1/auth/switch-tenant` | Issues tenant JWT |
| `POST /api/v1/auth/subscribe` | SaaS onboarding + tenant JWT |
| `GET /api/v1/platform/status` | Bootstrap status (anonymous) |

## Docker

```bash
docker compose -f backend/docker-compose.yml up --build
```

Image build: `docker build -f backend/Dockerfile -t kyvo-api .` from repository root.
