import type { OidcTokenResponse, SessionStorageLike } from '../types.js'

const SESSION_KEY = 'kyvo.client.session'

export interface PlatformTokens {
  accessToken: string
  refreshToken?: string
  expiresAt: number
}

export interface TenantTokens {
  accessToken: string
  expiresAt: number
  tenantId?: string
  membershipId?: string
}

export interface AuthSession {
  platform: PlatformTokens
  tenant?: TenantTokens
}

export class SessionManager {
  constructor(private readonly storage: SessionStorageLike) { }

  getSession(): AuthSession | null {
    const raw = this.storage.getItem(SESSION_KEY)
    if (!raw) return null
    try {
      const parsed = JSON.parse(raw) as AuthSession
      if (!parsed.platform?.accessToken) return null
      return parsed
    } catch {
      return null
    }
  }

  savePlatformTokens(tokens: OidcTokenResponse): AuthSession {
    const session: AuthSession = {
      platform: {
        accessToken: tokens.access_token,
        refreshToken: tokens.refresh_token,
        expiresAt: Date.now() + tokens.expires_in * 1000,
      },
    }
    this.storage.setItem(SESSION_KEY, JSON.stringify(session))
    return session
  }

  /** @deprecated Use savePlatformTokens */
  saveFromTokens(tokens: OidcTokenResponse): AuthSession {
    return this.savePlatformTokens(tokens)
  }

  updatePlatformTokens(tokens: OidcTokenResponse): AuthSession {
    const existing = this.getSession()
    const session: AuthSession = {
      platform: {
        accessToken: tokens.access_token,
        refreshToken: tokens.refresh_token ?? existing?.platform.refreshToken,
        expiresAt: Date.now() + tokens.expires_in * 1000,
      },
      tenant: existing?.tenant,
    }
    this.storage.setItem(SESSION_KEY, JSON.stringify(session))
    return session
  }

  /** @deprecated Use updatePlatformTokens */
  updateAccessToken(tokens: OidcTokenResponse): AuthSession {
    return this.updatePlatformTokens(tokens)
  }

  saveTenantToken(accessToken: string, expiresIn: number, tenantId?: string, membershipId?: string): AuthSession {
    const existing = this.getSession()
    if (!existing) {
      throw new Error('Platform session missing. Sign in via OIDC first.')
    }

    const session: AuthSession = {
      platform: existing.platform,
      tenant: {
        accessToken,
        expiresAt: Date.now() + expiresIn * 1000,
        tenantId,
        membershipId,
      },
    }
    this.storage.setItem(SESSION_KEY, JSON.stringify(session))
    return session
  }

  clearTenantToken(): void {
    const existing = this.getSession()
    if (!existing) return
    const session: AuthSession = { platform: existing.platform }
    this.storage.setItem(SESSION_KEY, JSON.stringify(session))
  }

  /** Returns tenant access token when active, otherwise null. */
  getAccessToken(): string | null {
    const session = this.getSession()
    if (!session) return null

    if (session.tenant && session.tenant.expiresAt > Date.now()) {
      return session.tenant.accessToken
    }

    return null
  }

  getPlatformAccessToken(): string | null {
    const session = this.getSession()
    if (!session) return null
    if (session.platform.expiresAt <= Date.now()) return null
    return session.platform.accessToken
  }

  getRefreshToken(): string | undefined {
    return this.getSession()?.platform.refreshToken
  }

  hasActiveTenantToken(): boolean {
    const session = this.getSession()
    return Boolean(session?.tenant && session.tenant.expiresAt > Date.now())
  }

  clear(): void {
    this.storage.removeItem(SESSION_KEY)
  }
}
