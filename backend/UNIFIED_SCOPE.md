# Backend Unificado — Baseline

## Dual-token (contrato)

| Token | Emissor | Claims |
|-------|---------|--------|
| Platform OIDC | `/connect/authorize` + `/connect/token` | `sub`, `email`, `name`, `sid`, `client_id`, `prole` — **sem** `tid`/`mid`/`trole` |
| Tenant JWT | `POST /api/v1/auth/switch-tenant`, `subscribe` | `tid`, `mid`, `trole`, `token_use=tenant`, `prole`, `sid`, `sub` |

## Exclusões em `/connect/*`

- Create/update tenant, membership CRUD, invites, applications registry
- Emissão de tenant JWT
- Qualquer regra de negócio além de AuthSession/`prole`/`sid`

## Decisões aplicadas

- TenancyKit removido → `ITenantContext` nativo + EF query filters + `TenantContextMiddleware`
- Backend unificado em `backend/` (`Kyvo.*`)
