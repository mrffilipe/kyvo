# Segurança — Kyvo IDP

## Desenvolvimento

- Use `dotnet user-secrets` para `Google:ClientId` / `Google:ClientSecret`.
- Não commitar `.env`, certificados com chave privada, nem `appsettings.*.local.json`.
- Perfil `https` em `launchSettings.json` (porta 5101).

## Produção

1. **HTTPS** terminado no host / reverse proxy; cookies com Secure.
2. **Certificado de assinatura/criptografia** OpenIddict:
   - `Oidc:SigningCertificatePath` (+ `Oidc:SigningCertificatePassword` se necessário)
   - Preferir Azure Key Vault / HSM em vez de PFX no disco.
3. **Issuer** (`Oidc:Issuer`) deve ser a URL pública canônica.
4. **CORS**: apenas origins dos clients registrados no OpenIddict (não abrir `*`).
5. **Segredos**: variáveis de ambiente / Key Vault; rotacionar Client Secrets do Google.
6. **Não** chamar `DisableTransportSecurityRequirement` fora de Development.
7. Manter validações padrão do OpenIddict (PKCE, audience, expiração).

## Logging

Serilog registra login local, federado, falhas de linking e pipeline HTTP. Em produção, envie para um sink central e evite logar tokens/códigos.
