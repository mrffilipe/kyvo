# @kyvo-client/client

<p align="center">
  <img src="https://raw.githubusercontent.com/mrffilipe/kyvo/main/sdk/icon.png" alt="Kyvo" width="96" />
</p>

Browser SDK for **Kyvo product applications** — OIDC (authorization code + PKCE), dual-token session management, JWT claim helpers, and typed REST resources under `/api/v1/*`.

> **Pronunciation:** *Kyvo* is pronounced like **"Key"vo** — rhymes with English *key* plus *vo*.

## Install

```bash
npm install @kyvo-client/client
```

## What it does

| Area | Capabilities |
| --- | --- |
| OIDC | Authorization code + PKCE, token refresh, logout, userinfo (platform JWT) |
| Session | Platform + tenant token storage (`session.saveTenantToken`) |
| Claims | Parse `token_use`, `tid`, `trole`, `prole`; `hasTenant`, `requiresOnboarding` |
| REST v1 | Users, tenants, memberships, tenant roles, audit logs |

`POST /api/v1/auth/subscribe` is **not** included — that endpoint is for BFF / server use only ([`Kyvo.Client`](https://www.nuget.org/packages/Kyvo.Client) on NuGet).

## Dual-token flow (3.0)

1. Sign in via OIDC → platform access token (no `tid` in OIDC JWT).
2. Call `switchTenant(tenantId)` or receive `accessToken` from your BFF after subscribe.
3. Tenant-scoped APIs use the tenant JWT (`token_use=tenant`) automatically when active.

```ts
import { createKyvoClient, hasTenant } from '@kyvo-client/client'

const kyvo = createKyvoClient({
  authority: 'http://localhost:5000',
  oidc: {
    clientId: 'pulse-crm-web',
    redirectUri: 'http://localhost:5173/auth/callback',
    scopes: 'openid profile email offline_access kyvo_api',
  },
})

await kyvo.oidc.signInRedirect()
// On /auth/callback:
const tokens = await kyvo.oidc.handleCallback(code, state)
kyvo.session.savePlatformTokens(tokens)

if (!hasTenant(kyvo.getAccessToken()!)) {
  await kyvo.switchTenant(tenantIdFromYourApp)
}

const me = await kyvo.users.getMe()
```

## API surface (`/api/v1`)

| Resource | Methods |
| --- | --- |
| Auth | switch-tenant, sessions, revoke session, delete account |
| Users | me, me/memberships, PATCH me |
| Tenants | list (+ search), get, patch, key availability, invite, list/revoke invites, accept invite |
| Memberships | CRUD under `/tenants/{id}/memberships` |
| Tenant roles | list/create, patch, delete custom role |
| Audit logs | list, filter-options |

Admin-only endpoints (applications, platform bootstrap, user search) are **not** in this SDK.

## Exports

```ts
import {
  createKyvoClient,
  hasTenant,
  requiresOnboarding,
  hasTenantRole,
  parseAccessTokenClaims,
  OidcClient,
  SessionManager,
  KyvoApiError,
} from '@kyvo-client/client'
```

## Related documentation

- [Product SDK overview](https://github.com/mrffilipe/kyvo/blob/main/sdk/README.md) — full endpoint matrix, .NET packages
- [Pulse CRM sample](https://github.com/mrffilipe/kyvo/tree/main/samples/pulse-crm) — reference SPA consumer
- [Changelog](https://github.com/mrffilipe/kyvo/blob/main/sdk/CHANGELOG.md)
- [Issues](https://github.com/mrffilipe/kyvo/issues)

## License

MIT
