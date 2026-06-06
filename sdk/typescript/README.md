# Kyvo TypeScript SDK workspace

npm workspace for **`@kyvo-client/client`** (sources under `@kyvo/client/`) — browser SDK for product SPAs (e.g. [Pulse CRM](../../samples/pulse-crm/frontend/)).

Overview and endpoint matrix: [../README.md](../README.md).

## Layout

```
@kyvo/client/src/
├── client.ts           createKyvoClient
├── types/
│   ├── api.ts          REST v1 DTOs (product surface)
│   └── oidc.ts         UserInfo response
├── generated/
│   └── schema.ts       full v1 OpenAPI types (regenerate via npm run generate:types)
├── oidc/               PKCE + authorize/token/logout/userinfo
├── session/            SessionManager
├── claims/             JWT helpers (tid, trole, prole)
├── api/                HTTP client + paths
└── resources/          auth (+ deleteAccount), users, tenants (+ key availability), memberships, tenantRoles (+ delete), auditLogs (+ filter-options)
```

`auth/subscribe` is omitted by design (server / `Kyvo.Client` only).

## Commands

```bash
npm install
npm run build
npm test
npm run generate:types   # requires ../swagger-v1.json (see parent README)
```

## Local consumers

[Pulse CRM](../../samples/pulse-crm/frontend/) consumes `@kyvo-client/client` from npm (`^1.0.1`). For monorepo SDK development, temporarily use `file:` in the sample or link with `npm pack`.
