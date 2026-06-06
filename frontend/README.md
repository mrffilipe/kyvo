# Kyvo — Frontend

[English](./README.md) | [Português](./README.pt-BR.md)

Admin SPA for the Kyvo. Consumes the API via OIDC (authorization code + PKCE) and exposes the UI to manage tenants, memberships, applications, identity providers, and audit logs.

> Coding conventions and required patterns: see [../rules/frontend-rules.md](../rules/frontend-rules.md).

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
- Backend running at `VITE_API_BASE_URL` (see configuration)
- Bootstrap credentials configured in the backend (`Bootstrap` in appsettings or `Bootstrap__*` env vars)
- If the platform has not been bootstrapped yet, the frontend itself runs the bootstrap from the `/login` screen (**Initialize platform** button)

---

## Configuration

Every variable below has a built-in default in `src/config/env.ts`, so the SPA runs in local development without an `.env` file. To override defaults locally, copy `.env.example` to `.env`:

```bash
cp .env.example .env
```

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_API_BASE_URL` | `http://localhost:5000` | Backend API base URL |
| `VITE_API_VERSION` | `1.0` | API version (produces `/v1.0/...`) |
| `VITE_API_TIMEOUT_MS` | `30000` | Axios request timeout (ms) |
| `VITE_OAUTH_CLIENT_ID` | `platform-admin-web` | OAuth client registered in Kyvo |
| `VITE_OAUTH_REDIRECT_URI` | `http://localhost:3000/auth/callback` | OIDC callback URI |

Defaults are kept in sync with the backend constants (`PlatformDefaults.AdminConsole.ClientId` and `DefaultRedirectUris`) and `appsettings.Development.json` — change them together.

### Docker image

Production image: [`Dockerfile`](./Dockerfile) → `mrffilipe/kyvo-frontend` (nginx on port 80, HTTP only). The default build uses empty `VITE_API_BASE_URL` and `VITE_OAUTH_REDIRECT_URI`; in the browser the app uses `window.location.origin` and `{origin}/auth/callback` when the external proxy serves API and SPA on the same public host.

Optional build-args override API and redirect URLs for split-host deployments (see comments in the Dockerfile).

Set `Jwt__Issuer` on the **API** service `.env` to your public URL (see [GETTING_STARTED.md §7](../GETTING_STARTED.md#7-production-deployment-docker-compose)).

**Production deploy:** [../GETTING_STARTED.md § Production](../GETTING_STARTED.md#7-production-deployment-docker-compose). **Build/push:** [../docs/DOCKER_PUBLISH.md](../docs/DOCKER_PUBLISH.md).

---

## Run

```bash
# Install dependencies
npm install

# Development (port 3000)
npm run dev

# Production build
npm run build

# Preview the build
npm run preview
```

---

## Bootstrap and authentication flow

```
1. The user opens the app (e.g., / or /login)
2. loginLoader / requireAuthLoader call GET /v1.0/platform/status
3. If requiresBootstrap → /login shows "Initialize platform" → POST /v1.0/platform/bootstrap
4. After bootstrap → the same route shows the OIDC login
```

### OIDC (after bootstrap)

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

---

## Pages and routes

| Route | Component | Auth | Description |
|-------|-----------|------|-------------|
| `/login` | `LoginPage` | Public | Bootstrap (when `requiresBootstrap`) or kicks off the OIDC flow |
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
| `platformService.ts` | `getPlatformStatus`, `bootstrapPlatform` |
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
| Firebase | `projectId`, `webApiKey`, `serviceAccount` | GoogleSocial | Admin UI form fields; `serviceAccount` = Admin SDK service account `.json` file (not the Web app `firebaseConfig`) |
| Cognito | `userPoolId`, `region`, `clientId` | GenericOidc | notice: login not yet available |
| Generic | `issuer`, `jwksUri`, `audience` | GenericOidc | notice: login not yet available |

TypeScript types that mirror the schemas live in `src/types/identityProviders.ts` (`FirebaseProviderConfig`, `IdpCapability`, etc.). `LoginPage.tsx` of the admin SPA does **not** change the OIDC flow — it only redirects to the backend authorize endpoint. Self-signup for end users is owned by Kyvo at `/account/register`, never by client apps.

---

## Swagger / OpenAPI

The `swagger.json` file at the root of the project contains the OpenAPI specification of the current API. It serves as a reference contract for the TypeScript types under `src/types/`.
