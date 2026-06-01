# TenancyKit integration (product APIs)

Uses the [TenancyKit](https://www.nuget.org/packages/TenancyKit) NuGet package (`>= 1.0.4 < 2.0`), referenced by `Kyvo.AspNetCore.TenancyKit`.

## Pipeline order

```text
UseAuthentication → UseMultiTenancy → UseAuthorization
```

## Kyvo JWT claims

| Claim | Use |
|-------|-----|
| `tid` | Tenant id (Guid) — TenancyKit resolver default |
| `mid` | Membership id — `IKyvoUserContext` only |
| `trole` | Tenant roles |
| `prole` | Platform roles |

**Important:** Kyvo's own `TenantStore` resolves by `Tenant.Key` (slug). Product apps with `tid` Guid should use `UseClaimPassthroughTenantStore()` from `Kyvo.AspNetCore.TenancyKit`, not the Kyvo host store.

## Example

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

Compatible with **TenancyKit >= 1.0.4 < 2.0**.
