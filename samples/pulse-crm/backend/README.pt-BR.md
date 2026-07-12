# PulseCRM API — autenticação e autorização (Kyvo)

[English](./README.md) | [Português](./README.pt-BR.md)

API de exemplo que valida JWTs emitidos pela Kyvo e chama `POST /api/v1/auth/subscribe` para vincular tenant + plano (`ApplicationTenant`).

## 1. Registrar Application e Client no painel

| Campo | Valor |
|-------|-------|
| Application slug | `pulse-crm` |
| Client ID | `pulse-crm-web` |
| Tipo | Public |
| Redirect URI | `http://localhost:5173/auth/callback` |
| Scopes | `openid profile email offline_access` |

`appsettings.Development.json` deve apontar para o mesmo issuer/audience da plataforma:

```json
"Kyvo": {
  "Authority": "https://localhost:5101",
  "Audience": "kyvo-api"
}
```

## 2. Fluxo OAuth2 / OIDC (SPA)

```
1. SPA → GET {Authority}/connect/authorize?client_id=...&code_challenge=...&scope=openid+profile+email+offline_access
2. Usuário autentica em /account/login OU cria conta em /account/register (cadastro central na Kyvo; este sample não tem tela própria de cadastro)
3. Redirect → http://localhost:5173/auth/callback?code=...&state=...
4. SPA → POST {Authority}/connect/token (authorization_code + code_verifier)
5. SPA armazena platform access_token + refresh_token
6. SPA → POST PulseCRM /api/onboarding/complete (Bearer platform token)
7. PulseCRM → POST {Authority}/api/v1/auth/subscribe (repassa o Bearer de plataforma)
8. Resposta inclui tenant JWT (`accessToken`); SPA chama `session.saveTenantToken` — não renove OIDC esperando `tid`
```

## 3. Validação JWT nesta API

- **Authority** = URL do issuer (mesmo valor de `Jwt:Issuer` na API Kyvo; aqui em `Kyvo:Authority`, ex. `https://localhost:5101`)
- **Audience** = `kyvo-api` (claim `aud` do access token; `Kyvo:Audience` no appsettings)
- Chaves públicas via JWKS: `{Authority}/.well-known/jwks.json`

Claims úteis no access token:

| Claim | Uso no CRM |
|-------|------------|
| `uid` / `sub` | Identificador do usuário |
| `email` | Perfil |
| `tid` | Tenant ativo (obrigatório para `/api/contacts`) |
| `mid` | Membership no tenant |
| `trole` | Papéis no tenant |
| `prole` | Papéis de plataforma |

Sem tenant JWT após login: o usuário ainda não fez subscribe ou não persistiu o `accessToken` retornado.

## 4. Pacotes SDK Kyvo (NuGet `3.1.0`)

| Pacote | Papel nesta API |
|--------|-----------------|
| `Kyvo.AspNetCore` | Validação JWT, `IKyvoUserContext`, policies |
| `Kyvo.Client` | `IKyvoProductClient.Auth.SubscribeAsync` |

Referenciados como `ProjectReference` no monorepo (ou pacotes NuGet). Isolamento de tenant via filtro EF nativo em `PulseCrmDbContext` com `IKyvoUserContext.TenantId` (sem TenancyKit).

## 5. `auth/subscribe` e ApplicationTenant

`POST /api/onboarding/complete` chama a plataforma:

```http
POST /api/v1/auth/subscribe
Authorization: Bearer {platform_access_token}
Content-Type: application/json

{
  "tenantName": "Acme Corp",
  "tenantKey": "acme-corp",
  "planCode": "professional",
  "externalCustomerId": "pay_mock_..."
}
```

A plataforma cria:

- **Tenant** + membership (owner) para o usuário da sessão OAuth
- **ApplicationTenant** ligando a application do client `pulse-crm-web` ao tenant, com `planCode` e `externalCustomerId`
- Retorna **tenant JWT** em `accessToken` (`token_use=tenant`) — a SPA deve persistir; refresh OIDC sozinho nunca adiciona `tid`

O CRM persiste cópia local em SQLite (`Subscriptions`) para exibir plano no dashboard.

## 6. Troubleshooting

| Erro | Causa | Solução |
|------|-------|---------|
| `invalid_scope` / `offline_access` | Client sem scope | Adicionar `offline_access` nos allowed scopes |
| 401 na API CRM | Audience/issuer incorretos | Conferir `Kyvo:Authority` e `Kyvo:Audience` (`https://localhost:5101` no HTTPS local) |
| Contatos vazios / sem tenant | Sem tenant JWT | Persistir `accessToken` de subscribe / switch-tenant |
| Subscribe 403/400 | Sessão sem client OAuth | Login via authorize do client `pulse-crm-web` |

## Endpoints locais

| Método | Rota |
|--------|------|
| GET | `/api/health` |
| GET | `/api/me` |
| POST | `/api/onboarding/complete` |
| GET | `/api/subscription` |
| CRUD | `/api/contacts` |
