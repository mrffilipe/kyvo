# Guia E2E — Kyvo IDP

## Pré-requisitos

- API em `https://localhost:5101`
- Postgres rodando (`docker compose up postgres`)
- Client seed `kyvo-idp-spa`

## 1. Discovery

```bash
curl.exe -sk https://localhost:5101/.well-known/openid-configuration
```

Confirme endpoints authorize/token/userinfo/revoke/introspect.

## 2. Login local + Authorization Code + PKCE

1. Gere PKCE (qualquer gerador S256).
2. Abra no browser:

```
https://localhost:5101/connect/authorize?client_id=kyvo-idp-spa&response_type=code&scope=openid%20profile%20email%20offline_access&redirect_uri=https://oauth.pstmn.io/v1/callback&code_challenge=<CHALLENGE>&code_challenge_method=S256
```

3. Login com `admin@kyvo.local` / `ChangeMe!123` (ou registre novo).
4. Aceite consentimento.
5. No Postman, `POST /connect/token`:

| Campo | Valor |
|-------|--------|
| grant_type | authorization_code |
| code | (do redirect) |
| redirect_uri | https://oauth.pstmn.io/v1/callback |
| client_id | kyvo-idp-spa |
| code_verifier | (seu verifier) |

6. `GET /connect/userinfo` com `Authorization: Bearer <access_token>`.
7. `POST /connect/token` com `grant_type=refresh_token`.
8. `POST /connect/revoke` com o token.

## 3. Login Google

1. Configure User Secrets do Google.
2. Redirect URI no Google Cloud: `https://localhost:5101/callback/login/google`
3. Repita o authorize; na tela de login escolha **Continuar com Google**.
4. Confirme que os tokens finais são emitidos por `https://localhost:5101/` (iss Kyvo), não pelo Google.
5. Segunda visita: mesma conta (reuse). Conta local pré-existente com mesmo e-mail verificado: link automático.

## 4. Health

```bash
curl.exe -sk https://localhost:5101/health
```

Esperado: `Healthy`.
