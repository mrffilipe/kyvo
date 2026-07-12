# Kyvo .NET SDK

The Kyvo .NET SDK helps you build **product APIs** (BFFs and backend services) that authenticate users via Kyvo OIDC and call the Kyvo REST API v1. It is not intended for the Kyvo admin console.

Cross-language overview and endpoint matrix: [../README.md](../README.md).

## Architecture

| Package | Responsibility |
| --- | --- |
| `Kyvo.AspNetCore` | JWT validation, `IKyvoUserContext`, and authorization policies for incoming requests. |
| `Kyvo.Client` | Typed HTTP client for Kyvo REST v1 — `SubscribeAsync`, users, tenants, memberships, roles, audit logs. |
| `Kyvo.Client.Tests` | Contract tests against the OpenAPI snapshot (not published). |

Runtime flow:

```text
Browser SPA (OIDC PKCE)
  -> platform JWT on switch-tenant / subscribe
  -> tenant JWT (token_use=tenant) on BFF / tenant APIs
  -> Kyvo.AspNetCore validates JWT → IKyvoUserContext
  -> product DbContext applies EF filters with IKyvoUserContext.TenantId
  -> Kyvo.Client calls Kyvo REST with the appropriate token
```

## Install

```bash
dotnet add package Kyvo.AspNetCore --version 3.1.0
dotnet add package Kyvo.Client --version 3.1.0
```

## Kyvo.AspNetCore

Register JWT authentication and user context in `Program.cs`:

```csharp
builder.Services.AddKyvoAuthentication(options =>
{
    options.Authority = builder.Configuration["Kyvo:Authority"]!;
    options.Audience = "kyvo-api";
});

builder.Services.AddAuthorization(options =>
{
    options.AddKyvoPolicies();
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

`IKyvoUserContext` exposes the authenticated user id, tenant id (`tid`), membership id (`mid`), tenant roles (`trole`), and platform roles (`prole`) from the access token.

Use `[Authorize(Policy = KyvoAuthorizationPolicies.RequireTenantToken)]` on endpoints that require a tenant JWT (`token_use=tenant`). Platform OIDC tokens are valid for user profile and tenant listing; tenant-scoped APIs need a tenant token from switch-tenant or subscribe.

### Product EF filtering (no TenancyKit)

Apply a query filter in your own `DbContext` using `IKyvoUserContext.TenantId` (see [Pulse CRM](../../samples/pulse-crm/backend/)):

```csharp
modelBuilder.Entity<Contact>().HasQueryFilter(c =>
    _userContext.TenantId == null || c.TenantId == _userContext.TenantId);
```

Configuration (`appsettings.json`):

```json
{
  "Kyvo": {
    "Authority": "https://localhost:5101"
  }
}
```

## Kyvo.Client

Register the typed client and call Kyvo from your BFF with the user's access token:

```csharp
builder.Services.AddKyvoClient(builder.Configuration);
// Kyvo:Authority — REST paths use /api/v1

var platformToken = httpContextAccessor.GetPlatformAccessToken();
var result = await kyvo.Auth.SubscribeAsync(
    platformToken!,
    new SubscribeTenantRequest("Acme", "acme"));
// result.Context.AccessToken is the tenant JWT for tenant-scoped APIs

var tenantToken = httpContextAccessor.GetTenantAccessToken();
var logs = await kyvo.AuditLogs.ListAsync(tenantToken!, cancellationToken);
```

`POST /auth/subscribe` is intentionally **server-only** — browsers should not call it directly. Use `Kyvo.Client` from your BFF.

Models live in `Kyvo.Client.Models` and match the Kyvo OpenAPI contract.

## Verification

```bash
dotnet build sdk/dotnet/Kyvo.sln
dotnet test sdk/dotnet/Kyvo.sln
```

## Related documentation

- [Product SDK overview](../README.md) — endpoint matrix, TypeScript client
- [Pulse CRM sample](../../samples/pulse-crm/) — reference consumer
- [SDK publishing](../../docs/SDK_PUBLISH.md) — maintainers
- [Repository](https://github.com/mrffilipe/kyvo)
