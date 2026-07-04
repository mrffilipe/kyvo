/**
 * Aligned with `#/components/schemas/IdentityProviderType`. Federation is implemented entirely through
 * OpenIddict.Client: Google/Microsoft/GitHub use the built-in OpenIddict.Client.WebIntegration presets;
 * GenericOidc covers any other OIDC-compliant provider (Cognito, Auth0, Keycloak, ...) via its own
 * discovery document.
 */
export const IdentityProviderType = {
  Local: 'Local',
  Google: 'Google',
  Microsoft: 'Microsoft',
  GitHub: 'GitHub',
  GenericOidc: 'GenericOidc',
} as const

export type IdentityProviderType = (typeof IdentityProviderType)[keyof typeof IdentityProviderType]

/** Authentication capabilities advertised by an identity provider on the login page. */
export const IdpCapability = {
  LocalPassword: 'LocalPassword',
  GoogleSocial: 'GoogleSocial',
  MicrosoftSocial: 'MicrosoftSocial',
  AppleSocial: 'AppleSocial',
  GenericOidc: 'GenericOidc',
} as const

export type IdpCapability = (typeof IdpCapability)[keyof typeof IdpCapability]

export interface IdentityProviderDto {
  id: string
  alias: string
  displayName: string
  providerType: IdentityProviderType
  enabled: boolean
  capabilities: IdpCapability[]
}

/** `AddIdentityProviderBody` in OpenAPI. */
export interface AddIdentityProviderBody {
  alias: string
  displayName: string
  providerType: IdentityProviderType
  capabilities: IdpCapability[]
  configJson?: string | null
}

/** `UpdateIdentityProviderBody` in OpenAPI. */
export interface UpdateIdentityProviderBody {
  displayName: string
  capabilities?: IdpCapability[] | null
  configJson?: string | null
}

/** Result returned by POST /v1.0/IdentityProviders. */
export interface AddIdentityProviderResult {
  id: string
  warnings: string[]
}

/**
 * Single configuration schema shared by every non-local provider type — mirrors `FederatedProviderConfig`
 * on the backend. `issuer` is only required for `GenericOidc`; Google/Microsoft/GitHub resolve their
 * issuer/discovery endpoint from the OpenIddict.Client.WebIntegration preset.
 */
export interface FederatedProviderConfig {
  clientId: string
  clientSecret: string
  issuer?: string
}
