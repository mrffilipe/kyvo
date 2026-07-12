# Kyvo — Frontend

[English](./README.md) | [Português](./README.pt-BR.md)

Admin SPA for the Kyvo. Consumes the API via OIDC (authorization code + PKCE) and exposes the UI to manage tenants, memberships, applications, identity providers, and audit logs.

> Coding conventions: see [frontend/README.md](../frontend/README.md).

---

## Stack

| Technology | Version | Use |
|------------|---------|-----|
| React | 19 | UI |
| React Router | 7 (Data mode) | Routing with loaders |
| Material UI | 9 | Design system |
| Axios | 1.x | HTTP client |
| TypeScript | 6 | Static typing |
| Vite | 8 | Build and dev server |

---

## Prerequisites

- Node.js (compatible with the version declared in `package.json`)
- Backend running at `VITE_API_BASE_URL` (see [GETTING_STARTED.md §3–4](../GETTING_STARTED.md#3-configure-the-backend))
- Bootstrap credentials in `backend/.env` (`Bootstrap__*`); the API initializes the platform on startup

---

## Configuration

Local development uses [docker-compose.yml](./docker-compose.yml) and [`.env.example`](./.env.example). Copy the example file and adjust ports or API URL if needed:

```bash
cp .env.example .env
```

| Variable | Default | Description |
|----------|---------|-------------|
| `FRONTEND_PORT` | `3000` | Host port mapped to the Vite dev server in the container |
| `VITE_API_BASE_URL` | `https://localhost:5101` | Backend API base URL |
| `VITE_API_TIMEOUT_MS` | `30000` | Axios request timeout (ms) |
| `VITE_OAUTH_CLIENT_ID` | `platform-admin-web` | OAuth client registered in Kyvo |
| `VITE_OAUTH_REDIRECT_URI` | `http://localhost:3000/auth/callback` | OIDC callback URI |

Defaults match the API on HTTPS port `5101` (`dotnet run --launch-profile https`; Docker Compose may still use `5000`) and the SPA on port `3000`. Change them only if you use different ports.

Built-in defaults also exist in `src/config/env.ts` when running Vite on the host without Docker (see **Run** below).

Defaults are kept in sync with the backend constants (`PlatformDefaults.AdminConsole.ClientId` and `DefaultRedirectUris`) and `backend/.env` — change them together.

### Docker image

Production image: [`Dockerfile`](./Dockerfile) → `mrffilipe/kyvo-frontend` (nginx on port 80, HTTP only). The default build uses empty `VITE_API_BASE_URL` and `VITE_OAUTH_REDIRECT_URI`; in the browser the app uses `window.location.origin` and `{origin}/auth/callback` when the external proxy serves API and SPA on the same public host.

Optional build-args override API and redirect URLs for split-host deployments (see comments in the Dockerfile).

Set `Jwt__Issuer` on the **API** service `.env` to your public URL (see [GETTING_STARTED.md §7](../GETTING_STARTED.md#7-production-deployment-docker-compose)).

**Production deploy:** [../GETTING_STARTED.md § Production](../GETTING_STARTED.md#7-production-deployment-docker-compose). **Build/push:** [../docs/DOCKER_PUBLISH.md](../docs/DOCKER_PUBLISH.md).

---

## Run

**Recommended (Docker Compose)** — see [GETTING_STARTED.md §4](../GETTING_STARTED.md#4-configure-and-start-the-frontend):

```bash
cd frontend
cp .env.example .env
docker compose up
```

The admin SPA runs at `http://localhost:3000`.

**Alternative (Vite on the host)** — for frontend-only work with the API already running:

```bash
npm install
npm run dev    # http://localhost:3000
npm run build
npm run preview
```

---

## Authentication flow

```
1. The user opens the app (e.g., / or /login)
2. loginLoader / requireAuthLoader call GET /api/v1/platform/status
3. If requiresBootstrap → /login shows a message to configure Bootstrap on the backend and restart the API
4. Otherwise → LoginPage starts the OIDC flow
```

### OIDC

```
1. The user navigates to a protected route
2. requireAuthLoader checks the status and the local storage (kyvo.auth.session)
3. If there is no session → redirect to /login?returnUrl=...
4. LoginPage → redirectToOidcLogin()
5. Browser navigates to GET /connect/authorize (PKCE, state in sessionStorage)
6. Backend redirects to /account/login (email + password form)
7. The user signs in locally → session cookie
8. Backend completes the authorize → redirect to /auth/callback?code=...&state=...
9. AuthCallbackPage validates the state, POST /connect/token (code + verifier)
10. Tokens saved in localStorage (kyvo.auth.session)
11. Redirect to the original route (returnUrl)
```

The refresh token is rotated automatically via an Axios interceptor when a request returns 401.

Logout clears `localStorage` and redirects to `GET /connect/logout`.

### Dual-token (3.1)

The admin SPA stores **both** tokens in `kyvo.auth.session`:

| Field | Source | Use |
|-------|--------|-----|
| `platformAccessToken` | OIDC `/connect/token` | Applications, IdPs, `switch-tenant`, platform admin APIs |
| `tenantAccessToken` | `POST /api/v1/auth/switch-tenant` (`accessToken`) | Memberships, Roles, Audit, invites, other `RequireTenantToken` routes |

[`TenantsPage.tsx`](src/pages/TenantsPage.tsx) persists the returned tenant JWT (no OIDC refresh after select). Axios attaches the tenant JWT when present, except for platform-only paths. On 401, OIDC refresh renews the platform token and **re-applies** `switch-tenant` if a tenant remains selected.

Do not expect `tid` on the OIDC access token; obtain tenant context via switch-tenant (or subscribe on the BFF in product apps).

---

## Pages and routes

| Route | Component | Auth | Description |
|-------|-----------|------|-------------|
| `/login` | `LoginPage` | Public | Message when `requiresBootstrap`, otherwise starts OIDC flow |
| `/auth/callback` | `AuthCallbackPage` | Public | Exchanges code for tokens |
| `/` | `HomePage` | JWT + plat_admin | Dashboard with module links |
| `/profile` | `ProfilePage` | JWT + plat_admin | User profile and memberships |
| `/sessions` | `SessionsPage` | JWT + plat_admin | List and revoke sessions |
| `/tenants` | `TenantsPage` | JWT + plat_admin | Tenant CRUD, invites, tenant switching |
| `/memberships` | `MembershipsPage` | JWT + plat_admin | Memberships and pending invites (copy link, revoke) |
| `/tenant-roles` | `TenantRolesPage` | JWT + plat_admin | Tenant-scoped role configuration |
| `/applications` | `ApplicationsPage` | JWT + plat_admin | List and create OAuth applications |
| `/applications/:id` | `ApplicationDetailPage` | JWT + plat_admin | Details, OAuth clients, provisioning |
| `/identity-providers` | `IdentityProvidersPage` | JWT + plat_admin | CRUD of identity providers |
| `/accept-invite` | `AcceptInvitePage` | JWT + plat_admin | Accept a tenant invite by token |
| `/audit-logs` | `AuditLogsPage` | JWT + plat_admin | Audit logs with filters |
| `/jwks` | `JwksPage` | JWT + plat_admin | Display the platform's JWKS |

---

## Folder structure

```
src/
├── components/
│   ├── AppLayout.tsx       Main shell with sidebar and topbar
│   ├── AuthLayout.tsx      Centered layout for auth screens
│   └── ui/                 Reusable components (DataTable, PageHeader, etc.)
├── config/
│   ├── axios.ts            Axios instances (api / publicApi) + 401 interceptor
│   ├── env.ts              Env variable loader with built-in defaults
│   └── index.ts            Re-exports
├── contexts/
│   ├── AuthContext.tsx      Authentication state (JWT claims, platform/tenant roles)
│   ├── TenantContext.tsx    Currently selected tenant (localStorage)
│   └── ThemeModeContext.tsx Light/dark theme
├── pages/                  One component per route
├── hooks/                  Shared hooks (debounced availability, tenant roles)
├── routes/
│   └── loaders.ts          Route loaders (requireAuthLoader, loginLoader)
├── routes.tsx              All routes defined with React Router
├── services/               API call functions per resource
├── theme/                  Tokens and createAppTheme (MUI)
├── types/                  TypeScript interfaces aligned with the OpenAPI document
└── utils/
    ├── authStorage.ts      Read/write session in localStorage
    ├── apiError.ts         Extract API error messages
    ├── apiMappers.ts       Normalize API responses (camelCase)
    ├── pkce.ts             Generate code_verifier and code_challenge
    └── jwt.ts              JWT decoding (no validation)
```

---

## API services

| File | Functions |
|------|-----------|
| `platformService.ts` | `getPlatformStatus` |
| `authService.ts` | `subscribeTenant`, `switchTenant`, `listActiveSessions`, `revokeSession` |
| `usersService.ts` | `getMe`, `updateMe`, `searchUsers`, `listMyMemberships` |
| `tenantsService.ts` | `createTenant`, `listTenants`, `getTenantById`, `updateTenant`, `inviteMember`, `listInvitesByTenant`, `revokeInvite`, `acceptInvite` |
| `membershipsService.ts` | `createMembership`, `listMembershipsByTenant`, `updateMembershipRole`, `revokeMembership` |
| `tenantRolesService.ts` | `listTenantRoles`, `createTenantRole`, `updateTenantRole` |
| `applicationsService.ts` | `createApplication`, `listApplications`, `getApplicationById`, `createApplicationClient`, `provisionApplicationTenant` |
| `identityProvidersService.ts` | `listIdentityProviders`, `addIdentityProvider`, `updateIdentityProvider`, `enableIdentityProvider`, `disableIdentityProvider` |
| `auditLogsService.ts` | `listAuditLogs` (with filters) |
| `wellKnownService.ts` | `getOpenIdConfiguration`, `getJwks` |
| `oidcService.ts` | `redirectToOidcLogin`, `redeemAuthorizationCode`, `refreshOidcTokens`, `buildLogoutUrl` |

---

## Frontend authorization

The admin console is restricted to users with the `plat_admin` platform role (`prole` JWT claim). Enforcement happens at three layers:

1. **OIDC** — the `platform-admin-web` client rejects authorization and token issuance for non-admins (`error=access_denied`).
2. **Route loaders** — `requireAuthLoader` clears the session and redirects to `/login?error=access_denied&error_description=…` when `plat_admin` is missing. `LoginPage` shows a dedicated message (no automatic OIDC redirect) with a **Try another account** button.
3. **Callback / Axios** — `AuthCallbackPage` validates claims after login; the API interceptor redirects on 403 when the session lacks `plat_admin`.

Use `isPlatformAdministrator()` from `authStorage.ts` (or `platformRoles.includes('plat_admin')` via `AuthContext`) for UI-level checks:

```tsx
const { platformRoles } = useAuth()
const isPlatformAdmin = platformRoles.includes('plat_admin')
```

Some actions (Identity Providers nav, create application, create tenant) remain gated in the UI for clarity, but the entire SPA requires `plat_admin` to enter.

### Identity Providers — `ConfigJson` schemas and capabilities

The **Identity Providers** page (`IdentityProvidersPage.tsx`) guides the operator by type and now collects `IdpCapability` flags (LocalPassword / GoogleSocial / MicrosoftSocial / AppleSocial / GenericOidc) via checkboxes. The form locks `LocalPassword` to the Local provider; backend `warnings` from social conflicts are surfaced in a dismissible alert.

| Type | JSON fields | Default capabilities | UI note |
|------|-------------|----------------------|---------|
| Local | none required | LocalPassword (locked) | no `ConfigJson` |
| Google | `clientId`, `clientSecret` | GoogleSocial | `FederatedProviderConfigForm` |
| Microsoft | `clientId`, `clientSecret` | MicrosoftSocial | OAuth redirect |
| GitHub | `clientId`, `clientSecret` | GoogleSocial | OAuth redirect |
| GenericOidc | `clientId`, `clientSecret`, `issuer` | GenericOidc | issuer required |

TypeScript types mirror the schemas in `src/types/identityProviders.ts` (`FederatedProviderConfig`, `IdpCapability`, etc.). `LoginPage.tsx` of the admin SPA does **not** change the OIDC flow — it only redirects to the backend authorize endpoint. Self-signup for end users is owned by Kyvo at `/account/register`, never by client apps.

---

## Swagger / OpenAPI

The `swagger.json` file at the project root (gitignored) is a local OpenAPI snapshot for TypeScript types under `src/types/`. Regenerate when the API changes:

```bash
curl https://localhost:5101/swagger/v1/swagger.json -o swagger.json
```
