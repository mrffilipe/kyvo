# @kyvo-client/client

<p align="center">
  <img src="https://raw.githubusercontent.com/mrffilipe/kyvo/main/sdk/icon.png" alt="Kyvo" width="96" />
</p>

Browser SDK for **Kyvo product applications** — OIDC (authorization code + PKCE), session management, JWT claim helpers, and typed REST v1 resources.

> **Pronunciation:** *Kyvo* is pronounced like **"Key"vo** — rhymes with English *key* plus *vo*.

## Install

```bash
npm install @kyvo-client/client
```

## What it does

| Area | Capabilities |
| --- | --- |
| OIDC | Authorization code + PKCE, token refresh, logout, userinfo |
| Session | Token storage, session manager |
| Claims | Parse `tid`, `trole`, `prole`; `hasTenant`, `requiresOnboarding` |
| REST v1 | Users, tenants, memberships, tenant roles, audit logs |

`POST /auth/subscribe` is **not** included — that endpoint is for BFF / server use only ([`Kyvo.Client`](https://www.nuget.org/packages/Kyvo.Client) on NuGet).

## Quick start

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
// On /auth/callback:
const tokens = await kyvo.oidc.handleCallback(code, state)
kyvo.session.saveFromTokens(tokens)

if (!hasTenant(kyvo.getAccessToken()!)) {
  await kyvo.refreshAccessTokenWithTenant()
}

const me = await kyvo.users.getMe()
```

## API surface (v1.0)

| Resource | Methods |
| --- | --- |
| Auth | switch-tenant, sessions, revoke session, delete account |
| Users | me, me/memberships, PATCH me |
| Tenants | list (+ search), get, patch, key availability, invite, list/revoke invites, accept invite |
| Memberships | CRUD under `/tenants/{id}/memberships` |
| Tenant roles | list/create, patch, delete custom role |
| Audit logs | list, filter-options |

Admin-only endpoints (applications, platform bootstrap, user search) are **not** in this SDK.

Paths use prefix `/v1.0/` (configurable via `apiVersion`).

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
