# Kyvo — Product SDK

SDK for **product applications** (SPAs and consumer APIs), not the admin console.

| Package | Role |
|---------|------|
| `@kyvo-client/client` | Browser: OIDC (PKCE), session, JWT claims, REST v1 |
| `Kyvo.AspNetCore` | API: JWT validation, `IKyvoUserContext`, authorization policies |
| `Kyvo.Client` | Server: `SubscribeAsync` + typed REST (BFF) |
| `Kyvo.AspNetCore.TenancyKit` | Optional EF multi-tenant bridge (TenancyKit + `tid` claim) |

**Versioning:** SemVer per package, aligned with API `v1.0`. See [CHANGELOG.md](CHANGELOG.md).

**Publishing (maintainers):** [docs/SDK_PUBLISH.md](../docs/SDK_PUBLISH.md) — GitHub Actions workflow, NuGet + npm secrets, manual release.

**API contract:** DTOs and routes follow the running Kyvo OpenAPI specs (`/swagger/v1/swagger.json`, `/swagger/oidc/swagger.json`). A snapshot for codegen lives in [`swagger-v1.json`](swagger-v1.json).

## Who calls what (typical CRM)

| Kyvo resource | SPA (`@kyvo-client/client`) | Product API (.NET SDK) |
|--------------|------------------------------|-------------------------|
| OIDC login / refresh / logout / userinfo | Yes | No (validates JWT only) |
| `POST /auth/subscribe` | **No** | **Yes** (BFF) |
| `auth/switch-tenant`, sessions | Yes | Optional |
| users, tenants, memberships, roles, audit | Yes | Optional |
| applications, Kyvo admin, platform bootstrap | No | No |

## Endpoint matrix (v1.0)

| Area | Methods | TS | .NET Client |
|------|---------|----|-------------|
| Auth | switch-tenant, sessions, revoke session | Yes | Yes |
| Auth | subscribe | **No** | **Yes** |
| Users | me, me/memberships, PATCH me | Yes | Yes |
| Tenants | list (+ optional `search`), get, patch, invites, accept invite | Yes | Yes |
| Memberships | CRUD under `/tenants/{id}/memberships` | Yes | Yes |
| Tenant roles | list/create under tenant; patch role | Yes | Yes |
| Audit logs | list (filters: userId, action, resourceType, from, to) | Yes | Yes |

Admin-only endpoints (`GET /Users` search, `POST /Tenants`, Applications, Identity Providers, Platform) are **not** in these SDKs — use the Kyvo admin UI / `frontend` services.

Paths use prefix `/v1.0/` (configurable via `apiVersion` / `KyvoClientOptions.ApiVersion`).

## TypeScript (`@kyvo-client/client`)

Browser package: OIDC (authorization code + PKCE), session storage, JWT claim helpers, and typed REST resources.

- Hand-written DTOs: [`typescript/@kyvo/client/src/types/api.ts`](typescript/@kyvo/client/src/types/api.ts), [`types/oidc.ts`](typescript/@kyvo/client/src/types/oidc.ts)
- Full OpenAPI mirror (reference): [`typescript/@kyvo/client/src/generated/schema.ts`](typescript/@kyvo/client/src/generated/schema.ts)

### Build and test

```bash
cd typescript
npm install
npm run build
npm test
```

Regenerate `schema.ts` after API changes (Kyvo API running):

```bash
# from repo root, refresh snapshot then codegen
curl -o sdk/swagger-v1.json http://localhost:5000/swagger/v1/swagger.json
cd sdk/typescript && npm run generate:types
```

### Quick start

```ts
import { createKyvoClient, hasTenant } from '@kyvo-client/client'

const kyvo = createKyvoClient({
  authority: 'http://localhost:5000',
  apiVersion: '1.0',
  oidc: {
    clientId: 'pulse-crm-web',
    redirectUri: 'http://localhost:5173/auth/callback',
    scopes: 'openid profile email offline_access',
  },
})

await kyvo.oidc.signInRedirect()
// /auth/callback → kyvo.oidc.handleCallback(code, state)
kyvo.session.saveFromTokens(tokens)

if (!hasTenant(kyvo.getAccessToken()!)) {
  await kyvo.refreshAccessTokenWithTenant()
}
```

`POST /auth/subscribe` is intentionally **not** on `@kyvo-client/client` (BFF / `Kyvo.Client` only).

See [typescript/README.md](typescript/README.md).

## .NET (`Kyvo.Client`)

Register and call from a BFF with the user's access token:

```csharp
builder.Services.AddKyvoClient(builder.Configuration);
// Kyvo:Authority, optional Kyvo:ApiVersion (default 1.0)

var token = KyvoClientServiceCollectionExtensions.GetUserAccessToken(httpContextAccessor);
var result = await kyvo.Auth.SubscribeAsync(token!, new SubscribeTenantRequest("Acme", "acme"));
```

Models live in `Kyvo.Client.Models` and match Swagger (e.g. `PagedResult.Total`, invite/membership bodies use `roles`, sessions use `sessionId`).

```bash
dotnet build sdk/dotnet/Kyvo.sln
dotnet test sdk/dotnet/Kyvo.sln
```

## TenancyKit

Product APIs with EF should prefer `Kyvo.AspNetCore.TenancyKit` over manual `tid` filtering. See [dotnet/TENANCYKIT.md](dotnet/TENANCYKIT.md).

## Samples

[Pulse CRM](../samples/pulse-crm/) is the reference consumer.
