import { createKyvoClient } from '@kyvo-client/client'
import { env } from './env'

export const kyvoClient = createKyvoClient({
  authority: env.kyvoAuthority,
  oidc: {
    clientId: env.kyvoClientId,
    redirectUri: env.kyvoRedirectUri,
    scopes: env.kyvoScopes,
  },
})
