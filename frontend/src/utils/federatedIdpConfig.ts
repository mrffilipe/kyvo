import type { FederatedProviderConfig, IdentityProviderType } from '../types'

export const BOOTSTRAP_LOCAL_IDP_ALIAS = 'local'

export function isBootstrapLocalProvider(providerType: string, alias: string): boolean {
  return providerType === 'Local' || alias.trim().toLowerCase() === BOOTSTRAP_LOCAL_IDP_ALIAS
}

export interface FederatedConfigFieldValues {
  clientId: string
  clientSecret: string
  /** Only used/required when providerType === GenericOidc. */
  issuer: string
}

export const emptyFederatedConfigFields = (): FederatedConfigFieldValues => ({
  clientId: '',
  clientSecret: '',
  issuer: '',
})

export function buildFederatedConfigJson(values: FederatedConfigFieldValues): string {
  const config: FederatedProviderConfig = {
    clientId: values.clientId.trim(),
    clientSecret: values.clientSecret.trim(),
    ...(values.issuer.trim() ? { issuer: values.issuer.trim() } : {}),
  }
  return JSON.stringify(config)
}

export function validateFederatedConfigFields(
  providerType: IdentityProviderType,
  values: FederatedConfigFieldValues,
  mode: 'create' | 'update',
): string | null {
  const clientId = values.clientId.trim()
  const clientSecret = values.clientSecret.trim()
  const issuer = values.issuer.trim()
  const requiresIssuer = providerType === 'GenericOidc'

  if (mode === 'create') {
    if (!clientId) {
      return 'Informe o Client ID.'
    }
    if (!clientSecret) {
      return 'Informe o Client Secret.'
    }
    if (requiresIssuer && !issuer) {
      return 'Informe o Issuer (URL base do provedor OIDC) para provedores genéricos.'
    }
    return null
  }

  const anyFilled = Boolean(clientId || clientSecret || issuer)
  if (!anyFilled) {
    return null
  }
  if (!clientId || !clientSecret) {
    return 'Para alterar a configuração, preencha o Client ID e o Client Secret.'
  }
  if (requiresIssuer && !issuer) {
    return 'Informe o Issuer (URL base do provedor OIDC) para provedores genéricos.'
  }
  return null
}
