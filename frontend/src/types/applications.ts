import type { PagedResult } from './common'

/** Alinhado a `#/components/schemas/ApplicationType` (JsonStringEnumConverter no backend). */
export const ApplicationType = {
  Web: 'Web',
  Mobile: 'Mobile',
  Backend: 'Backend',
} as const

export type ApplicationType = (typeof ApplicationType)[keyof typeof ApplicationType]

/** Alinhado a `#/components/schemas/ClientType`. */
export const ClientType = {
  Public: 'Public',
  Confidential: 'Confidential',
} as const

export type ClientType = (typeof ClientType)[keyof typeof ClientType]

export interface ApplicationClientSummary {
  id: string
  clientId: string
  clientType: ClientType
  redirectUris: string[]
  postLogoutRedirectUris: string[]
  allowedScopes: string[]
  accessTokenTtlSeconds: number
  isSystem: boolean
}

export interface Application {
  id: string
  name: string
  slug: string
  type: ApplicationType
  isSystem: boolean
  brandingEnabled: boolean
  brandingPrimaryColor: string | null
  brandingSecondaryColor: string | null
  brandingLogoUrl: string | null
  brandingHeroTitle: string | null
  brandingHeroSubtitle: string | null
  clients?: ApplicationClientSummary[]
}

export interface ApplicationBranding {
  applicationId: string
  brandingEnabled: boolean
  brandingPrimaryColor: string | null
  brandingSecondaryColor: string | null
  brandingLogoUrl: string | null
  brandingHeroTitle: string | null
  brandingHeroSubtitle: string | null
}

export interface UpdateApplicationBrandingBody {
  brandingEnabled: boolean
  brandingPrimaryColor?: string | null
  brandingSecondaryColor?: string | null
  brandingHeroTitle?: string | null
  brandingHeroSubtitle?: string | null
}

/** `CreateApplicationBody` no OpenAPI. */
export interface CreateApplicationBody {
  name: string
  slug: string
  type: ApplicationType
}

export interface CreateApplicationResponse {
  id: string
}

/** `CreateApplicationClientBody` no OpenAPI. */
export interface CreateApplicationClientBody {
  clientId: string
  clientSecretHash?: string | null
  clientType: ClientType
  redirectUris: string
  postLogoutRedirectUris?: string
  allowedScopes?: string
  allowedScopesList?: string[]
  accessTokenTtlSeconds: number
}

export interface CreateApplicationClientResponse {
  id: string
}

/** `ProvisionApplicationTenantBody` no OpenAPI. */
export interface ProvisionApplicationTenantBody {
  tenantName: string
  tenantKey: string
  initialAdministratorUserId?: string | null
  initialAdministratorEmail?: string | null
  externalCustomerId?: string | null
  planCode?: string | null
}

export interface ProvisionApplicationTenantResponse {
  applicationId: string
  tenantId: string
  membershipId: string
}

export type ListApplicationsResponse = PagedResult<Application>
