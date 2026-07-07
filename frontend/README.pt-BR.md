# Kyvo — Frontend

[English](./README.md) | [Português](./README.pt-BR.md)

Painel administrativo (SPA) do Kyvo. Consome a API via OIDC (authorization code + PKCE) e expõe interface para gestão de tenants, memberships, applications, identity providers e audit logs.

> Convenções de código: ver [frontend/README.md](../frontend/README.md).

---

## Stack

| Tecnologia | Versão | Uso |
|------------|--------|-----|
| React | 19 | UI |
| React Router | 7 (Data Mode) | Roteamento com loaders |
| Material UI | 9 | Design system |
| Axios | 1.x | HTTP client |
| TypeScript | 6 | Tipagem estática |
| Vite | 8 | Build e dev server |

---

## Pré-requisitos

- Node.js (versão compatível com `package.json`)
- Backend rodando em `VITE_API_BASE_URL` (veja [GETTING_STARTED.pt-BR.md §3–4](../GETTING_STARTED.pt-BR.md#3-configurar-o-backend))
- Credenciais de bootstrap em `backend/.env` (`Bootstrap__*`); a API inicializa a plataforma na subida

---

## Configuração

O desenvolvimento local usa [docker-compose.yml](./docker-compose.yml) e [`.env.example`](./.env.example). Copie o exemplo e ajuste portas ou URL da API se necessário:

```bash
cp .env.example .env
```

| Variável | Default | Descrição |
|----------|---------|-----------|
| `FRONTEND_PORT` | `3000` | Porta no host mapeada para o Vite no container |
| `VITE_API_BASE_URL` | `http://localhost:5000` | URL base da API backend |
| `VITE_API_TIMEOUT_MS` | `30000` | Timeout das requisições Axios (ms) |
| `VITE_OAUTH_CLIENT_ID` | `platform-admin-web` | Client OAuth registrado na Kyvo |
| `VITE_OAUTH_REDIRECT_URI` | `http://localhost:3000/auth/callback` | URI de callback OIDC |

Os defaults batem com a API na porta `5000` e o SPA na `3000`. Altere só se usar outras portas.

Defaults embutidos também existem em `src/config/env.ts` ao rodar o Vite no host sem Docker (veja **Como rodar** abaixo).

Os defaults são mantidos em sincronia com as constantes do backend (`PlatformDefaults.AdminConsole.ClientId` e `DefaultRedirectUris`) e o `backend/.env` — mude todos juntos.

### Imagem Docker

Imagem de produção: [`Dockerfile`](./Dockerfile) → `mrffilipe/kyvo-frontend` (nginx na porta 80, somente HTTP). O build padrão deixa `VITE_API_BASE_URL` e `VITE_OAUTH_REDIRECT_URI` vazios; no navegador o app usa `window.location.origin` quando API e SPA compartilham o mesmo host público via proxy externo.

Build-args opcionais para hosts separados (veja comentários no Dockerfile). Defina `Jwt__Issuer` no `.env` do serviço **api** (veja [GETTING_STARTED.pt-BR.md §7](../GETTING_STARTED.pt-BR.md#7-deploy-em-produção-docker-compose)).

**Deploy em produção:** [../GETTING_STARTED.pt-BR.md § Produção](../GETTING_STARTED.pt-BR.md#7-deploy-em-produção-docker-compose). **Build/push:** [../docs/DOCKER_PUBLISH.pt-BR.md](../docs/DOCKER_PUBLISH.pt-BR.md).

---

## Como rodar

**Recomendado (Docker Compose)** — veja [GETTING_STARTED.pt-BR.md §4](../GETTING_STARTED.pt-BR.md#4-configurar-e-subir-o-frontend):

```bash
cd frontend
cp .env.example .env
docker compose up
```

O admin SPA fica em `http://localhost:3000`.

**Alternativa (Vite no host)** — para trabalho só no frontend com a API já rodando:

```bash
npm install
npm run dev    # http://localhost:3000
npm run build
npm run preview
```

---

## Fluxo de autenticação

```
1. Usuário acessa a aplicação (ex.: / ou /login)
2. loginLoader / requireAuthLoader consultam GET /api/v1/platform/status
3. Se requiresBootstrap → /login exibe mensagem para configurar Bootstrap no backend e reiniciar a API
4. Caso contrário → LoginPage inicia fluxo OIDC
### OIDC

```
1. Usuário acessa rota protegida
2. requireAuthLoader verifica status e localStorage (kyvo.auth.session)
3. Se sem sessão → redirect /login?returnUrl=...
4. LoginPage → redirectToOidcLogin()
5. Browser navega para GET /connect/authorize (PKCE, state em sessionStorage)
6. Backend redireciona para /account/login (formulário email + senha)
7. Usuário faz login local → cookie de sessão
8. Backend completa o authorize → redirect /auth/callback?code=...&state=...
9. AuthCallbackPage valida state, POST /connect/token (code + verifier)
10. Tokens salvos em localStorage (kyvo.auth.session)
11. Redirect para a rota original (returnUrl)
```

O refresh token é trocado automaticamente via interceptor Axios em respostas 401.

O logout limpa o `localStorage` e redireciona para `GET /connect/logout`.

---

## Páginas e rotas

| Rota | Componente | Auth | Descrição |
|------|-----------|------|-----------|
| `/login` | `LoginPage` | Público | Mensagem se `requiresBootstrap`, senão inicia fluxo OIDC |
| `/auth/callback` | `AuthCallbackPage` | Público | Troca código por token |
| `/` | `HomePage` | JWT + plat_admin | Dashboard com links para módulos |
| `/profile` | `ProfilePage` | JWT + plat_admin | Perfil e memberships do usuário |
| `/sessions` | `SessionsPage` | JWT + plat_admin | Listar e revogar sessões |
| `/tenants` | `TenantsPage` | JWT + plat_admin | CRUD de tenants, convites, switch de tenant |
| `/memberships` | `MembershipsPage` | JWT + plat_admin | Memberships e convites pendentes (copiar link, revogar) |
| `/tenant-roles` | `TenantRolesPage` | JWT + plat_admin | Papéis configuráveis do tenant |
| `/applications` | `ApplicationsPage` | JWT + plat_admin | Listar e criar applications OAuth |
| `/applications/:id` | `ApplicationDetailPage` | JWT + plat_admin | Detalhes, clients OAuth, provisioning |
| `/identity-providers` | `IdentityProvidersPage` | JWT + plat_admin | CRUD de provedores de identidade |
| `/accept-invite` | `AcceptInvitePage` | JWT + plat_admin | Aceitar convite de tenant via token |
| `/audit-logs` | `AuditLogsPage` | JWT + plat_admin | Logs de auditoria com filtros |
| `/jwks` | `JwksPage` | JWT + plat_admin | Exibir JWKS da plataforma |

---

## Estrutura de pastas

```
src/
├── components/
│   ├── AppLayout.tsx       Shell principal com sidebar e topbar
│   ├── AuthLayout.tsx      Layout centralizado para telas de auth
│   └── ui/                 Componentes reutilizáveis (DataTable, PageHeader, etc.)
├── config/
│   ├── axios.ts            Instâncias Axios (api / publicApi) + interceptor 401
│   ├── env.ts              Leitura de variáveis de ambiente com defaults embutidos
│   └── index.ts            Re-exportações
├── contexts/
│   ├── AuthContext.tsx      Estado de autenticação (JWT claims, platform/tenant roles)
│   ├── TenantContext.tsx    Tenant ativo selecionado (localStorage)
│   └── ThemeModeContext.tsx Tema claro/escuro
├── pages/                  Um componente por rota
├── hooks/                  Hooks compartilhados (disponibilidade com debounce, roles de tenant)
├── routes/
│   └── loaders.ts          Route loaders (requireAuthLoader, loginLoader)
├── routes.tsx              Definição de todas as rotas com React Router
├── services/               Funções de chamada à API por recurso
├── theme/                  Tokens e createAppTheme (MUI)
├── types/                  Interfaces TypeScript alinhadas ao OpenAPI
└── utils/
    ├── authStorage.ts      Leitura/escrita de sessão no localStorage
    ├── apiError.ts         Extração de mensagem de erro da API
    ├── apiMappers.ts       Normalização de respostas da API (camelCase)
    ├── pkce.ts             Geração de code_verifier e code_challenge
    └── jwt.ts              Decodificação de JWT (sem validação)
```

---

## Autorização frontend

O console admin é restrito a usuários com a role de plataforma `plat_admin` (claim `prole` no JWT). A proteção ocorre em três camadas:

1. **OIDC** — o client `platform-admin-web` recusa authorize e emissão de tokens para quem não é admin (`error=access_denied`).
2. **Route loaders** — `requireAuthLoader` limpa a sessão e redireciona para `/login?error=access_denied&error_description=…` quando falta `plat_admin`. A `LoginPage` exibe mensagem dedicada (sem redirecionar automaticamente ao OIDC) e o botão **Tentar com outra conta**.
3. **Callback / Axios** — `AuthCallbackPage` valida claims após o login; o interceptor da API redireciona em 403 quando a sessão não tem `plat_admin`.

Use `isPlatformAdministrator()` de `authStorage.ts` (ou `platformRoles.includes('plat_admin')` via `AuthContext`) para checagens na UI:

```tsx
const { platformRoles } = useAuth()
const isPlatformAdmin = platformRoles.includes('plat_admin')
```

Algumas ações (nav Identity Providers, criar application, criar tenant) continuam condicionadas na UI por clareza, mas todo o SPA exige `plat_admin` para entrar.

### Identity Providers — schemas `ConfigJson` e capabilities

A página **Identity Providers** (`IdentityProvidersPage.tsx`) orienta o cadastro por tipo e agora coleta flags `IdpCapability` (LocalPassword / GoogleSocial / MicrosoftSocial / AppleSocial / GenericOidc) via checkboxes. O form trava `LocalPassword` no provider Local; `warnings` de conflito retornados pelo backend são exibidos em alerta dispensável.

| Tipo | Campos no JSON | Capabilities default | Observação na UI |
|------|----------------|----------------------|------------------|
| Local | nenhum obrigatório | LocalPassword (locked) | sem `ConfigJson` |
| Google | `clientId`, `clientSecret` | GoogleSocial | `FederatedProviderConfigForm` |
| Microsoft | `clientId`, `clientSecret` | MicrosoftSocial | redirect OAuth |
| GitHub | `clientId`, `clientSecret` | GoogleSocial | redirect OAuth |
| GenericOidc | `clientId`, `clientSecret`, `issuer` | GenericOidc | issuer obrigatório |

Tipos TypeScript espelhando os schemas: `src/types/identityProviders.ts` (`FederatedProviderConfig`, `IdpCapability`, etc.). O `LoginPage.tsx` do painel **não** altera o fluxo OIDC — apenas redireciona para o authorize do backend. Self-signup de usuários finais é responsabilidade da Kyvo em `/account/register`, nunca dos apps cliente.

---

## Swagger / OpenAPI

O arquivo `swagger.json` na raiz do projeto contém a especificação OpenAPI da API atual. Serve como contrato de referência para os tipos TypeScript em `src/types/`.
