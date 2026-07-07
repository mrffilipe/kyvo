export type { PagedResult, TenantContextResult } from './types/api.js'

export interface OidcTokenResponse {
  access_token: string
  token_type: string
  expires_in: number
  refresh_token?: string
  id_token?: string
  scope?: string
}

export interface KyvoClientConfig {
  authority: string
  oidc: {
    clientId: string
    redirectUri: string
    scopes?: string
    storage?: SessionStorageLike
    pkceStorage?: SessionStorageLike
  }
}

export interface SessionStorageLike {
  getItem(key: string): string | null
  setItem(key: string, value: string): void
  removeItem(key: string): void
}

export interface AuthSession {
  platform: {
    accessToken: string
    refreshToken?: string
    expiresAt: number
  }
  tenant?: {
    accessToken: string
    expiresAt: number
    tenantId?: string
    membershipId?: string
  }
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
}
