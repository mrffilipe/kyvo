# Kyvo .NET SDK

The Kyvo .NET SDK helps you build **product APIs** (BFFs and backend services) that authenticate users via Kyvo OIDC and call the Kyvo REST API v1. It is not intended for the Kyvo admin console.

Cross-language overview and endpoint matrix: [../README.md](../README.md).

## Architecture

The solution is split into small packages so consumers can reference only the pieces they need:

| Package | Responsibility |
| --- | --- |
| `Kyvo.AspNetCore` | JWT validation, `IKyvoUserContext`, and authorization policies for incoming requests. |
| `Kyvo.Client` | Typed HTTP client for Kyvo REST v1 — `SubscribeAsync`, users, tenants, memberships, roles, audit logs. |
| `Kyvo.AspNetCore.TenancyKit` | Optional bridge to [TenancyKit](https://www.nuget.org/packages/TenancyKit) for EF Core multi-tenancy using the `tid` JWT claim. |
| `Kyvo.Client.Tests` | Contract tests against the OpenAPI snapshot (not published). |

Runtime flow:

```text
Browser SPA (OIDC PKCE)
  -> access token on BFF requests
  -> Kyvo.AspNetCore validates JWT
  -> optional Kyvo.AspNetCore.TenancyKit resolves tenant from tid claim
  -> Kyvo.Client calls Kyvo REST API with the user's token
```

## Install

```bash
dotnet add package Kyvo.AspNetCore --version 2.0.0
dotnet add package Kyvo.Client --version 2.0.0
# optional EF multi-tenant:
dotnet add package Kyvo.AspNetCore.TenancyKit --version 2.0.0
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

Configuration (`appsettings.json`):

```json
{
  "Kyvo": {
    "Authority": "https://idp.example.com"
  }
}
```

## Kyvo.Client

Register the typed client and call Kyvo from your BFF with the user's access token:

```csharp
builder.Services.AddKyvoClient(builder.Configuration);
// Kyvo:Authority, optional Kyvo:ApiVersion (default 1.0)

// In a controller or minimal API handler:
var token = KyvoClientServiceCollectionExtensions.GetUserAccessToken(httpContextAccessor);
var result = await kyvo.Auth.SubscribeAsync(
    token!,
    new SubscribeTenantRequest("Acme", "acme"));
```

`POST /auth/subscribe` is intentionally **server-only** — browsers should not call it directly. Use `Kyvo.Client` from your BFF.

Models live in `Kyvo.Client.Models` and match the Kyvo OpenAPI contract (e.g. `PagedResult.Total`, invite bodies use `roles`).

## Kyvo.AspNetCore.TenancyKit

For product APIs with Entity Framework Core, prefer `Kyvo.AspNetCore.TenancyKit` over manual `tid` filtering.

Pipeline order:

```text
UseAuthentication -> UseMultiTenancy -> UseAuthorization
```

| Claim | Use |
| --- | --- |
| `tid` | Tenant id (Guid) — TenancyKit resolver default |
| `mid` | Membership id — `IKyvoUserContext` only |
| `trole` | Tenant roles |
| `prole` | Platform roles |

Example:

```csharp
builder.Services
    .AddKyvoAuthentication(o => { o.Authority = "https://idp.example"; o.Audience = "kyvo-api"; })
    .AddKyvoTenancyKit<PulseTenantInfo>(options =>
    {
        options.UseMissingTenantBehavior(MissingTenantBehavior.Throw);
        options.UseClaimsTenantResolver("tid");
        options.UseClaimPassthroughTenantStore();
        options.ConfigureEntity<ITenantOwned, Guid>(e => e.TenantId);
    });

app.UseAuthentication();
app.UseMultiTenancy<PulseTenantInfo>();
app.UseAuthorization();
```

Full integration guide: [TENANCYKIT.md](TENANCYKIT.md).

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
