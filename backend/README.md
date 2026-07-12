# Kyvo Backend

Identity Provider (OpenIddict + ASP.NET Core Identity) with multi-tenant domain APIs.
**No TenancyKit** — native `ITenantContext` + EF query filters.

## Dual-token

```
OIDC (/connect/*)                     API (/api/v1/*)
─────────────────                     ──────────────
authorize → token                     switch-tenant / subscribe → tenant JWT
Platform: sub, email, sid, prole      Tenant: tid, mid, trole, token_use=tenant
```

`/connect/*` never emits `tid`/`mid`/`trole`.

## Projects

| Project | Role |
|---------|------|
| `Kyvo.Domain` | Entities, VOs, repository ports |
| `Kyvo.Application` | Use cases, queries, ports |
| `Kyvo.Infrastructure` | EF, Identity, OpenIddict, native tenancy |
| `Kyvo.API` | OIDC host + `/api/v1` |
| `Kyvo.Tests` | Dual-token contract tests |

See [UNIFIED_SCOPE.md](UNIFIED_SCOPE.md), [SECURITY.md](SECURITY.md), [docs/E2E.md](docs/E2E.md).

## Run locally

```bash
cd backend
docker compose up postgres -d
dotnet ef database update --project Kyvo.Infrastructure --startup-project Kyvo.API
dotnet run --project Kyvo.API --launch-profile https
```

- HTTPS: `https://localhost:5101`
- Health: `/health`
- Discovery: `/.well-known/openid-configuration`
- Platform: `/api/v1/platform/status`

Dev bootstrap: `admin@kyvo.local` / `ChangeMe!123`
