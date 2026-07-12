# Kyvo — Product SDK

SDK for **product applications** (SPAs and consumer APIs), not the admin console.

| Package | Role |
|---------|------|
| `@kyvo-client/client` | Browser: OIDC (PKCE), session, JWT claims, REST v1 |
| `Kyvo.AspNetCore` | API: JWT validation, `IKyvoUserContext`, authorization policies |
| `Kyvo.Client` | Server: `SubscribeAsync` + typed REST (BFF) |

**Versioning:** SemVer per package, aligned with API `/api/v1`. Current release: **3.1.0** (dual-token; TenancyKit removed). See [CHANGELOG.md](CHANGELOG.md).

**Publishing (maintainers):** [docs/SDK_PUBLISH.md](../docs/SDK_PUBLISH.md) — GitHub Actions workflow, NuGet + npm secrets, manual release.

**API contract:** DTOs and routes follow the running Kyvo OpenAPI specs (`/swagger/v1/swagger.json`, `/swagger/oidc/swagger.json`). Snapshots: [`swagger-v1.json`](swagger-v1.json), [`swagger-oidc.json`](swagger-oidc.json).

## Who calls what (typical CRM)

| Kyvo resource | SPA (`@kyvo-client/client`) | Product API (.NET SDK) |
|--------------|------------------------------|-------------------------|
| OIDC login / refresh / logout / userinfo | Yes | No (validates JWT only) |
| `POST /auth/subscribe` | **No** | **Yes** (BFF) |
| `auth/switch-tenant`, sessions | Yes | Optional |
| users, tenants, memberships, roles, audit | Yes | Optional |
| applications, Kyvo admin, platform bootstrap | No | No |

## Endpoint matrix (v1)

| Area | Methods | TS | .NET Client |
|------|---------|----|-------------|
| Auth | switch-tenant, sessions, revoke session, **delete account** | Yes | Yes |
| Auth | subscribe | **No** | **Yes** |
| Users | me, me/memberships, PATCH me | Yes | Yes |
| Tenants | list (+ optional `search`), get, patch, **key availability**, invite (`acceptPath`), **list/revoke invites**, accept invite | Yes | Yes |
| Memberships | CRUD under `/tenants/{id}/memberships` | Yes | Yes |
| Tenant roles | list/create under tenant; patch role; **delete** custom role | Yes | Yes |
| Audit logs | list + **filter-options** | Yes | Yes |

Admin-only endpoints are **not** in these SDKs — use the Kyvo admin UI / `frontend` services.

Paths use prefix `/api/v1/`.

## TypeScript (`@kyvo-client/client`)

Browser package: OIDC (authorization code + PKCE), dual-token session storage, JWT claim helpers, and typed REST resources.

### Build and test

```bash
cd typescript
npm install
npm run build
npm test
```

Regenerate `schema.ts` after API changes (Kyvo API running):

```bash
# from repo root — local HTTPS profile defaults to 5101
curl -k -o sdk/swagger-v1.json https://localhost:5101/swagger/v1/swagger.json
cd sdk/typescript && npm run generate:types
```

### Quick start (two-step auth)

```ts
import { createKyvoClient, hasTenant } from '@kyvo-client/client'

const kyvo = createKyvoClient({
  authority: 'https://localhost:5101',
  oidc: {
    clientId: 'kyvo-spa',
    redirectUri: 'http://localhost:5173/auth/callback',
    scopes: 'openid profile email offline_access kyvo_api',
  },
})

// 1. OIDC login → platform token (no tid)
await kyvo.oidc.signInRedirect()
// /auth/callback → kyvo.session.savePlatformTokens(tokens)

// 2. Switch tenant → tenant-scoped JWT
const ctx = await kyvo.switchTenant(selectedTenantId)
// kyvo.getAccessToken() now returns the tenant token

if (!hasTenant(kyvo.getAccessToken()!)) {
  throw new Error('Tenant context missing after switch-tenant')
}
```

OIDC tokens are **pure** (no `tid`). Tenant context comes only from `POST /auth/switch-tenant`. To renew a tenant token, refresh the platform OIDC token and call `switchTenant` again.

`POST /auth/subscribe` is intentionally **not** on `@kyvo-client/client` (BFF / `Kyvo.Client` only).

See [typescript/README.md](typescript/README.md).

## .NET (`Kyvo.Client` + `Kyvo.AspNetCore`)

```csharp
builder.Services.AddKyvoClient(builder.Configuration);
// Kyvo:Authority

var platformToken = httpContextAccessor.GetPlatformAccessToken();
var result = await kyvo.Auth.SubscribeAsync(platformToken!, new SubscribeTenantRequest("Acme", "acme"));
// result.Context.AccessToken → tenant JWT; filter EF with IKyvoUserContext.TenantId
```

```bash
dotnet build sdk/dotnet/Kyvo.sln
dotnet test sdk/dotnet/Kyvo.sln
```

## Samples

[Pulse CRM](../samples/pulse-crm/) is the reference consumer (native EF filter via `IKyvoUserContext`, no TenancyKit).
