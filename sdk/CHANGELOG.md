# Changelog

All SDK packages follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [3.1.0] - 2026-07-12

### Removed

- **Breaking:** `Kyvo.AspNetCore.TenancyKit` package and docs. Product APIs must apply EF tenant filters locally using `IKyvoUserContext.TenantId` (see Pulse CRM sample).

### Changed

- Refresh OpenAPI snapshots (`swagger-v1.json`, `swagger-oidc.json`) and regenerate TypeScript `generated/schema.ts` from the unified backend.
- Document dual-token authority defaults (`https://localhost:5101` for local HTTPS profile).

## [3.0.0] - 2026-07-07

### Changed

- **Breaking:** REST paths use `/api/v1/*` (was `/v1.0/*`).
- **Breaking:** Dual-token model — platform OIDC JWT + tenant JWT (`token_use=tenant`) from `POST /api/v1/auth/switch-tenant` or `POST /api/v1/auth/subscribe`.
- **Breaking:** OIDC access tokens no longer include `tid` / `trole`; obtain tenant context via switch-tenant or subscribe.
- TypeScript: removed `refreshAccessTokenWithTenant`; use `switchTenant` and `session.saveTenantToken`.
- TypeScript: `createApiPaths()` no longer accepts `apiVersion`.
- .NET: `IKyvoProductClient` auth methods take `platformAccessToken`; tenant-scoped APIs take `tenantAccessToken`.
- .NET: `SubscribeAsync` / `SwitchTenantAsync` map `AccessToken`, `ExpiresIn`, `TokenType` on `TenantContextResult`.
- .NET: `KyvoSessionTokens`, `GetPlatformAccessToken`, `GetTenantAccessToken` helpers on `IHttpContextAccessor`.
- Remove `clientId` from `AuthSessionDto` (session listing no longer binds OAuth client server-side).
- TypeScript `parseAccessTokenClaims` and .NET `IKyvoUserContext.OAuthClientId` expose the `client_id` JWT claim for application context.

### Fixed

- Refresh `swagger-v1.json` and `generated/schema.ts` for `/api/v1` routes and dual-token auth responses.

## [2.0.0] - 2026-07-04

### Changed

- Platform auth stack: ASP.NET Core Identity + OpenIddict (consumer OIDC contract unchanged)
- Subscribe no longer returns inline tokens; refresh via `/connect/token`
- IdentityProviderType enum aligned with admin API

### Removed

- `SubscribeTenantResponse.Tokens` (.NET)

## [1.0.2] - 2026-06-06

### Added

- Tenants: `GET /Tenants/{id}/invites`, `DELETE /Invites/{id}`; `POST` invite returns `InviteMemberResult` / `InviteMemberResponse` with `acceptPath`.
- Auth: `DELETE /auth/account` (application-scoped account deletion).
- Tenants: `GET /Tenants/keys/{key}/availability`.
- Tenant roles: `DELETE /TenantRoles/{id}` for unused custom roles.
- Audit logs: `GET /AuditLogs/filter-options`.
- NuGet package readme and icon; XML API documentation on .NET packages.
- npm package readme with install guide and API surface overview.

### Fixed

- Align REST DTOs and request bodies with Kyvo OpenAPI (`PagedResult.total`, `roles` on invites/memberships, `AuthSessionDto`, `AuditLogItemDto`, `UserDto.memberships`, etc.).
- TypeScript: shared types in `types/api.ts`; regenerate `generated/schema.ts` from `swagger-v1.json`.
- .NET: JSON camelCase + string enums for API serialization; tenant list `search` and audit `userId` / `resourceType` filters.

### Changed

- npm package renamed from `@kyvo/client` to **`@kyvo-client/client`** (org [kyvo-client](https://www.npmjs.com/org/kyvo-client) on npm).
- Repository URL updated to `https://github.com/mrffilipe/kyvo`.
- NuGet metadata: `RepositoryUrl`, `PackageProjectUrl`, `PackageLicenseExpression`, package icon.

## [1.0.0] - 2026-05-30

### Added

- Initial product SDK: `@kyvo-client/client` (npm), `Kyvo.AspNetCore`, `Kyvo.Client`, `Kyvo.AspNetCore.TenancyKit`
- Subscribe only on .NET client (BFF pattern)
- Pulse CRM sample migration
