# Kyvo Backend — Scope

Canonical contract for the **unified backend** (OIDC + domain): see **[UNIFIED_SCOPE.md](./UNIFIED_SCOPE.md)** and **[README.md](./README.md)**.

## Included

- Local auth (email/password) via ASP.NET Core Identity
- Federated Google via OpenIddict.Client
- OpenIddict OP: Authorization Code + PKCE, refresh tokens
- Dual-token: platform OIDC JWT + tenant JWT from `switch-tenant` / `subscribe`
- Native multi-tenancy (`ITenantContext` + EF query filters) — **no TenancyKit**
- Domain APIs: tenants, memberships, roles, invites, audit, applications, IdPs
- Admin SPA + product SDKs consume this host

## OIDC endpoints

| Endpoint | Path |
|----------|------|
| Discovery | `/.well-known/openid-configuration` |
| Authorize | `/connect/authorize` |
| Token | `/connect/token` |
| UserInfo | `/connect/userinfo` |
| Logout | `/connect/logout` |
| Revoke | `/connect/revoke` |
| Introspect | `/connect/introspect` |
| Federated challenge | `/login/federated/{alias}` |
| Federated callback | `/callback/login/{alias}` |

## Explicitly out of scope

- MFA, SAML, SCIM, Device Code, Firebase Authentication
- `Kyvo.AspNetCore.TenancyKit` (removed; product BFFs filter via `IKyvoUserContext`)

## Local authority

| Mode | Base URL |
|------|----------|
| `dotnet run --launch-profile https` | `https://localhost:5101` |
| Docker Compose (default `API_PORT`) | `http://localhost:5000` |
