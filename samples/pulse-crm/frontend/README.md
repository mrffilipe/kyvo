# PulseCRM — Frontend (sample)

[English](./README.md) | [Português](./README.pt-BR.md)

Consumer SPA for the Pulse CRM sample. It drives the standard OIDC **authorization code + PKCE** flow against the Kyvo and calls the CRM API for onboarding, subscription, and contacts.

> Full end-to-end guide (API + test flow): [../README.md](../README.md)  
> Coding conventions: [../../../rules/frontend-rules.md](../../../rules/frontend-rules.md) (see §12 — central signup on Kyvo).

---

## Stack

| Technology | Version | Use |
|------------|---------|-----|
| React | 19 | UI |
| React Router | 7 | Routing (loaders + `RequireAuth`) |
| Material UI | 9 | Design system |
| `@kyvo-client/client` | npm `^1.0.1` | OIDC (PKCE), session, JWT claims, `users.getMe()`, token refresh |
| Axios | 1.x | Pulse CRM API only (not Kyvo REST) |
| TypeScript | 6 | Static typing |
| Vite | 8 | Dev server and build |

---

## Prerequisites

- Kyvo running at `http://localhost:5000` (bootstrap completed)
- Pulse CRM API running at `http://localhost:5100`
- OAuth client `pulse-crm-web` registered in the admin console (see [../../README.md](../../README.md))

---

## Configuration

Built-in defaults in `src/config/env.ts` let you run locally without an `.env` file. To override, copy `.env.example` to `.env`:

```bash
cp .env.example .env
```

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_KYVO_AUTHORITY` | `http://localhost:5000` | Kyvo issuer / authority |
| `VITE_KYVO_CLIENT_ID` | `pulse-crm-web` | OAuth public client |
| `VITE_KYVO_REDIRECT_URI` | `http://localhost:5173/auth/callback` | OIDC callback |
| `VITE_KYVO_SCOPES` | `openid profile email offline_access` | Requested scopes |
| `VITE_CRM_API_URL` | `http://localhost:5100` | Pulse CRM API base URL |

---

## Run

```bash
npm install
npm run dev      # http://localhost:5173
npm run build
npm run lint
npm run preview
```

The app consumes `@kyvo-client/client` from npm (`npm install` resolves `^1.0.1`).

---

## Authentication and signup

This SPA has **no `/register` route** and no local signup form. The login screen only redirects to `/connect/authorize`; sign-in and account creation happen on Kyvo domain:

- Existing users: `/account/login`
- New users: link on Kyvo login page to `/account/register` (central signup)

After the first token exchange, users without a `tid` claim are routed to **onboarding** → mock payment → `auth/subscribe` on the platform (BFF only). See [../README.md#test-flow](../README.md#test-flow).

### `@kyvo-client/client` usage in this sample

| Concern | Module |
|---------|--------|
| Client singleton | `src/config/kyvoClient.ts` → `createKyvoClient()` |
| Tokens / logout | `src/utils/kyvoSession.ts` → `kyvoClient.session` |
| OIDC login/callback | `kyvoClient.oidc.*` |
| Kyvo REST (demo) | `kyvoClient.users.getMe()` on dashboard + callback |
| CRM HTTP | `src/services/crmApi.ts` (Axios + Bearer from `kyvoClient.getAccessToken()`) |

`POST /auth/subscribe` is **not** called from the SPA — only from the .NET API via `Kyvo.Client`.

---

## Main routes

| Path | Purpose |
|------|---------|
| `/login` | Start OIDC redirect |
| `/auth/callback` | Exchange `code` for tokens |
| `/onboarding` | Plan + company name |
| `/payment` | Mock checkout → subscribe |
| `/dashboard` | Post-subscribe home |
| `/contacts` | Tenant-scoped CRUD |
