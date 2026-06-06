# Changelog

All SDK packages follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
