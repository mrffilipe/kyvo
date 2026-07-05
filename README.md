# Kyvo

[English](./README.md) | [Português](./README.pt-BR.md)

> **Pronunciation:** *Kyvo* is pronounced like **"Key"vo** — rhymes with English *key* plus *vo*.

An **identity and access platform (IdP)** for an ecosystem of applications: centralizes local authentication (ASP.NET Core Identity), issues JWT tokens via OpenIddict OIDC, organizes tenants (organizations), members, roles, OAuth applications, and supports federation with external providers (Google, Microsoft, GitHub, generic OIDC).

Inspired by Keycloak-style platforms: a managed, multi-tenant IdP with its own admin console.

---

## Getting started

| Path | Use |
|------|-----|
| **Development** | Clone the repo and follow [GETTING_STARTED.md](./GETTING_STARTED.md) sections **1–6** (Docker Compose, `.env`, PostgreSQL/Redis on the host) |
| **Production** | Deploy `kyvo-api` + `kyvo-frontend` — [GETTING_STARTED.md § Production](./GETTING_STARTED.md#7-production-deployment-docker-compose) (split images, external HTTPS proxy) |
| **Maintainers** | Build and push images — [docs/DOCKER_PUBLISH.md](./docs/DOCKER_PUBLISH.md) |

---

## Repository layout

```
backend/    ASP.NET Core 8 API (product 2.0.0) — Clean Architecture (Domain / Application / Infrastructure / API)
frontend/   Admin SPA 2.0.0 — React 19 + MUI + React Router 7 + Vite
samples/    Reference consumer applications (e.g., pulse-crm — SaaS CRM + OIDC)
docker/     API entrypoint script (split images: backend/ and frontend/ Dockerfiles)
docs/       Maintainer guides (e.g. Docker image publishing)
.github/    CI workflows (Docker publish on version tags)
```

---

## Documentation

| Document | Content |
|----------|---------|
| [GETTING_STARTED.md](./GETTING_STARTED.md) | Development (§1–6) and production Docker deploy (§7) |
| [docs/DOCKER_PUBLISH.md](./docs/DOCKER_PUBLISH.md) | Build/push images: CI workflow and manual commands |
| [backend/README.md](./backend/README.md) | Backend architecture, configuration, endpoints, migrations, OIDC |
| [frontend/README.md](./frontend/README.md) | Frontend stack, environment variables, OIDC flow, and pages |
| [samples/README.md](./samples/README.md) | Sample consumers and OAuth checklist in the admin console |
| [samples/pulse-crm/README.md](./samples/pulse-crm/README.md) | PulseCRM: how to run and test the OIDC integration |

---

## Product overview

| Concept | Description |
|---------|-------------|
| **Local IdP** | Email + password via ASP.NET Core Identity. Configured at bootstrap. |
| **OIDC** | OpenIddict server: authorization code + PKCE flow. RS256 tokens. Discovery at `/.well-known/openid-configuration`. |
| **Multi-tenant** | Users belong to multiple tenants with independent roles. |
| **Platform admin** | Users with `prole=plat_admin` manage tenants, applications, and global IdPs. |
| **OAuth applications** | Registry of consumer apps with public (PKCE) or confidential clients. |
| **Identity Providers** | Federation: Local (default), Google, Microsoft, GitHub, GenericOidc. Each declares `IdpCapability` flags with hard-lock for email/password and conflict warnings for socials. |
| **Self-registration** | Central `/account/register` page on the IdP with configurable password policy and rate limiting. Consumer apps redirect to OIDC; they never expose private signup endpoints. |
| **Modern login UI** | Login and register pages are served by Blazor Web App Static SSR. Federated providers use OAuth redirect via `/login/federated/{alias}` → `/callback/login/{alias}`. |
| **Secret protection** | IdP OAuth client secrets (`clientSecret` in `ConfigJson`) are encrypted at rest via ASP.NET Core Data Protection. |
| **Audit logs** | Event tracking per tenant. |

---

## Quick prerequisites

- .NET 8 SDK
- Node.js (LTS)
- PostgreSQL 14+
- Redis (optional)

For the full installation guide see [GETTING_STARTED.md](./GETTING_STARTED.md).
