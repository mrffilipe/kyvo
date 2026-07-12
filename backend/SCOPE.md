# Kyvo IDP — Baseline de escopo (Fase 0)

## Incluído

- Autenticação local (email/senha) via ASP.NET Core Identity
- Autenticação federada Google via OpenIddict.Client (direto, sem Firebase)
- OpenIddict como OpenID Provider (OP): Authorization Code + PKCE, Refresh Token
- Mapeamento de claims e vinculação de contas
- UI Blazor SSR: login, registro, consentimento
- Endpoints OIDC:

| Endpoint | Path |
|----------|------|
| Discovery | `/.well-known/openid-configuration` |
| JWKS | (via OpenIddict) |
| Authorize | `/connect/authorize` |
| Token | `/connect/token` |
| UserInfo | `/connect/userinfo` |
| Logout | `/connect/logout` |
| Revoke | `/connect/revoke` |
| Introspect | `/connect/introspect` |
| Federated challenge | `/login/federated/google` |
| Federated callback | `/callback/login/google` |

## Excluído (explícito)

- Multi-tenancy (`Tenant`, `Membership`, `TenantRole`)
- Dual-token / `switch-tenant` / `subscribe`
- AuthSession de produto, invites, audit de produto
- Admin SPA, SDKs, samples
- MFA, SAML, SCIM, Device Code, Firebase Authentication
- Regras de negócio fora de identidade

## Localização

Solução irmã em `idp/`; `backend/` permanece como referência intacta.

## Checklist de aceite por fase

| Fase | Critério |
|------|----------|
| 0 | Escopo e exclusões alinhados; path `idp/` |
| 1 | `dotnet build`; health em HTTPS |
| 2 | Migrações aplicam; UserManager autentica usuário local |
| 3 | Discovery ok; Authorization Code + PKCE emite tokens |
| 4 | Registro/login local + consentimento → tokens Kyvo |
| 5 | Login Google → tokens emitidos pelo Kyvo |
| 6 | Create/reuse/link por email; claims mapeadas |
| 7 | Secrets fora do git; Serilog; docker-compose |
| 8 | README permite E2E completo por terceiro |
