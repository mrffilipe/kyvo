# Kyvo

[English](./README.md) | [Português](./README.pt-BR.md)

> **Pronúncia:** *Kyvo* pronuncia-se como **"Key"vo** — parecido com a palavra inglesa *key* + *vo*.

Plataforma de **identidade e acesso (IdP)** para um ecossistema de aplicações: centraliza autenticação local (ASP.NET Core Identity), emite tokens JWT via OpenIddict OIDC, organiza tenants (organizações), membros, papéis, aplicações OAuth e suporta federação de provedores externos (Google, Microsoft, GitHub, OIDC genérico).

Inspirado no modelo Keycloak-like: um IdP gerenciado, multi-tenant, com painel administrativo próprio.

---

## Primeiros passos

| Caminho | Uso |
|---------|-----|
| **Desenvolvimento** | Clone o repo e siga [GETTING_STARTED.pt-BR.md](./GETTING_STARTED.pt-BR.md) seções **1–6** (Docker Compose, `.env`, PostgreSQL/Redis no host) |
| **Produção** | Imagens `kyvo-api` + `kyvo-frontend` — [GETTING_STARTED.pt-BR.md § Produção](./GETTING_STARTED.pt-BR.md#7-deploy-em-produção-docker-compose) (imagens separadas, proxy HTTPS externo) |
| **Mantenedores** | Build e push de imagens — [docs/DOCKER_PUBLISH.pt-BR.md](./docs/DOCKER_PUBLISH.pt-BR.md) |

---

## Estrutura do repositório

```
backend/    API ASP.NET Core 8 (produto 3.0.0) — Clean Architecture (Domain / Application / Infrastructure / API)
frontend/   Painel admin SPA 3.0.0 — React 19 + MUI + React Router 7 + Vite
samples/    Aplicações consumidoras de referência (ex.: pulse-crm — CRM SaaS + OIDC)
docker/     Entrypoint da API (Dockerfiles em backend/ e frontend/)
docs/       Guias para mantenedores (ex.: publicação Docker)
.github/    CI (publicação Docker em tags de versão)
```

---

## Documentação

| Documento | Conteúdo |
|-----------|----------|
| [GETTING_STARTED.pt-BR.md](./GETTING_STARTED.pt-BR.md) | Desenvolvimento (§1–6) e deploy Docker em produção (§7) |
| [docs/DOCKER_PUBLISH.pt-BR.md](./docs/DOCKER_PUBLISH.pt-BR.md) | Build/push de imagens: workflow CI e comandos manuais |
| [backend/README.md](./backend/README.md) | Arquitetura, configuração, endpoints, migrations e OIDC do backend |
| [frontend/README.md](./frontend/README.md) | Stack, variáveis de ambiente, fluxo OIDC e páginas do frontend |
| [samples/README.md](./samples/README.md) | Samples consumidores e checklist de OAuth no painel |
| [samples/pulse-crm/README.md](./samples/pulse-crm/README.md) | PulseCRM: como rodar e testar integração OIDC |

---

## Visão geral do produto

| Conceito | Descrição |
|----------|-----------|
| **IdP local** | Email + senha via ASP.NET Core Identity. Configurado no bootstrap. |
| **OIDC** | OpenIddict server: fluxo authorization code + PKCE. Tokens RS256. Discovery em `/.well-known/openid-configuration`. |
| **Multi-tenant** | Usuários pertencem a múltiplos tenants com papéis independentes. |
| **Platform admin** | Usuários com `prole=plat_admin` gerenciam tenants, applications e IdPs globais. |
| **Applications OAuth** | Registro de apps consumidoras com clients públicos (PKCE) ou confidenciais. |
| **Identity Providers** | Federação: Local (padrão), Google, Microsoft, GitHub, GenericOidc. Cada um declara flags `IdpCapability` com hard-lock para email/senha e warnings em conflito de socials. |
| **Self-registration** | Página central `/account/register` no IdP, com política de senha configurável e rate limit. Apps cliente redirecionam para OIDC; nunca expõem endpoint próprio de cadastro. |
| **UI moderna de login** | Login e cadastro renderizados via Blazor Web App Static SSR. Provedores federados usam redirect OAuth via `/login/federated/{alias}` → `/callback/login/{alias}`. |
| **Proteção de segredos** | Segredos OAuth dos IdPs (`clientSecret` em `ConfigJson`) são criptografados em repouso via ASP.NET Core Data Protection. |
| **Audit logs** | Rastreio de eventos por tenant. |

---

## Pré-requisitos rápidos

- .NET 8 SDK
- Node.js (LTS)
- PostgreSQL 14+
- Redis (opcional)

Para o guia completo de instalação: [GETTING_STARTED.pt-BR.md](./GETTING_STARTED.pt-BR.md).
