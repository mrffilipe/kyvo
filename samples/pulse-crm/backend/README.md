# PulseCRM API — authentication and authorization (Kyvo)

[English](./README.md) | [Português](./README.pt-BR.md)

Sample API that validates JWTs issued by the Kyvo and calls `POST /api/v1/auth/subscribe` to link tenant + plan (`ApplicationTenant`).

## 1. Register the Application and Client in the admin console

| Field | Value |
|-------|-------|
| Application slug | `pulse-crm` |
| Client ID | `pulse-crm-web` |
| Type | Public |
| Redirect URI | `http://localhost:5173/auth/callback` |
| Scopes | `openid profile email offline_access` |

`appsettings.Development.json` must point to the same issuer/audience as the platform:

```json
"Kyvo": {
  "Authority": "https://localhost:5101",
  "Audience": "kyvo-api"
}
```

## 2. OAuth2 / OIDC flow (SPA)

```
1. SPA → GET {Authority}/connect/authorize?client_id=...&code_challenge=...&scope=openid+profile+email+offline_access
2. The user authenticates at /account/login OR creates an account at /account/register (central Kyvo signup; no signup screen in this sample)
3. Redirect → http://localhost:5173/auth/callback?code=...&state=...
4. SPA → POST {Authority}/connect/token (authorization_code + code_verifier)
5. SPA stores platform access_token + refresh_token
6. SPA → POST PulseCRM /api/onboarding/complete (Bearer platform token)
7. PulseCRM → POST {Authority}/api/v1/auth/subscribe (forwards the platform Bearer)
8. Response includes tenant JWT (`accessToken`); SPA calls `session.saveTenantToken` — do not refresh OIDC expecting `tid`
```

## 3. JWT validation in this API

- **Authority** = issuer URL (same value as `Jwt:Issuer` on the Kyvo API, configured here as `Kyvo:Authority`, e.g. `https://localhost:5101`)
- **Audience** = `kyvo-api` (`aud` claim of the access token; `Kyvo:Audience` in appsettings)
- Public keys via JWKS: `{Authority}/.well-known/jwks.json`

Useful access-token claims:

| Claim | CRM usage |
|-------|-----------|
| `uid` / `sub` | User identifier |
| `email` | Profile |
| `tid` | Active tenant (required for `/api/contacts`) |
| `mid` | Membership in the tenant |
| `trole` | Tenant roles |
| `prole` | Platform roles |

No tenant JWT after login: the user has not subscribed yet, or has not called switch-tenant / saved `accessToken` from subscribe.

## 4. Kyvo SDK packages (NuGet `3.1.0`)

| Package | Role in this API |
|---------|------------------|
| `Kyvo.AspNetCore` | JWT validation, `IKyvoUserContext`, policies |
| `Kyvo.Client` | `IKyvoProductClient.Auth.SubscribeAsync` |

Referenced as monorepo `ProjectReference` in `PulseCrm.Api.csproj` (or published NuGet packages).

Tenant isolation uses a **native EF query filter** on `PulseCrmDbContext` driven by `IKyvoUserContext.TenantId` (from the tenant JWT `tid` claim). There is no TenancyKit dependency.

Onboarding returns `TenantContextResult` with inline `AccessToken` (tenant JWT) for the SPA to store (`OnboardingCompleteResponse`).

## 5. `auth/subscribe` and ApplicationTenant

`POST /api/onboarding/complete` calls the platform:

```http
POST /api/v1/auth/subscribe
Authorization: Bearer {platform_access_token}
Content-Type: application/json

{
  "tenantName": "Acme Corp",
  "tenantKey": "acme-corp",
  "planCode": "professional",
  "externalCustomerId": "pay_mock_..."
}
```

The platform creates:

- **Tenant** + membership (owner) for the user of the OAuth session
- **ApplicationTenant** linking the application of client `pulse-crm-web` to the tenant, with `planCode` and `externalCustomerId`
- Returns a **tenant JWT** in `accessToken` (`token_use=tenant`) — the SPA must persist it; OIDC refresh alone never adds `tid`

The CRM also stores a local copy in SQLite (`Subscriptions`) to display the plan on the dashboard.

## 6. Troubleshooting

| Error | Cause | Solution |
|-------|-------|----------|
| `invalid_scope` / `offline_access` | Client without the scope | Add `offline_access` to allowed scopes |
| 401 on the CRM API | Wrong audience/issuer | Check `Kyvo:Authority` and `Kyvo:Audience` (`https://localhost:5101` for local HTTPS) |
| Contacts empty / missing tenant | No tenant JWT | Persist `accessToken` from subscribe / switch-tenant |
| Subscribe 403/400 | Session without an OAuth client | Sign in via the authorize endpoint of the `pulse-crm-web` client |

## Local endpoints

| Method | Path |
|--------|------|
| GET | `/api/health` |
| GET | `/api/me` |
| POST | `/api/onboarding/complete` |
| GET | `/api/subscription` |
| CRUD | `/api/contacts` |
