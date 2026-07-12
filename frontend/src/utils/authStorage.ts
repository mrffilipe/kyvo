import type { AuthTenantSummary } from '../types'
import type { OidcTokenResponse, TenantContextResult } from '../types/oidc'
import { tryParseJwtPayload } from './jwt'
import { tokenExpiresAtIso } from './oidcToken'

const SESSION_STORAGE_KEY = 'kyvo.auth.session'

export interface AuthSessionStorage {
  /** Platform OIDC access token (no tid/mid/trole). */
  platformAccessToken: string
  /** Tenant JWT from switch-tenant / subscribe (token_use=tenant). */
  tenantAccessToken?: string | null
  refreshToken: string
  expiresAtIso: string
  tenantExpiresAtIso?: string | null
  userId: string
  email: string
  tenantId?: string | null
  membershipId?: string | null
  tenantRoles: string[]
  platformRoles: string[]
  tenants: AuthTenantSummary[]
}

/** Legacy shape before dual-token storage (migrated on read). */
interface LegacyAuthSessionStorage {
  accessToken?: string
  platformAccessToken?: string
  tenantAccessToken?: string | null
  refreshToken: string
  expiresAtIso: string
  tenantExpiresAtIso?: string | null
  userId: string
  email: string
  tenantId?: string | null
  membershipId?: string | null
  tenantRoles: string[]
  platformRoles: string[]
  tenants: AuthTenantSummary[]
}

interface AccessTokenClaims extends Record<string, unknown> {
  sub?: string
  uid?: string
  email?: string
  tid?: string
  mid?: string
  trole?: string | string[]
  prole?: string | string[]
}

function isBrowser(): boolean {
  return typeof window !== 'undefined'
}

function normalizeRoles(value: string | string[] | undefined): string[] {
  if (!value) {
    return []
  }
  return Array.isArray(value) ? value : [value]
}

function migrateSession(raw: LegacyAuthSessionStorage): AuthSessionStorage {
  const platformAccessToken = raw.platformAccessToken ?? raw.accessToken ?? ''
  return {
    platformAccessToken,
    tenantAccessToken: raw.tenantAccessToken ?? null,
    refreshToken: raw.refreshToken,
    expiresAtIso: raw.expiresAtIso,
    tenantExpiresAtIso: raw.tenantExpiresAtIso ?? null,
    userId: raw.userId,
    email: raw.email,
    tenantId: raw.tenantId ?? null,
    membershipId: raw.membershipId ?? null,
    tenantRoles: raw.tenantRoles ?? [],
    platformRoles: raw.platformRoles ?? [],
    tenants: raw.tenants ?? [],
  }
}

export function getAuthSession(): AuthSessionStorage | null {
  if (!isBrowser()) {
    return null
  }

  const raw = localStorage.getItem(SESSION_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as LegacyAuthSessionStorage
    const session = migrateSession(parsed)
    if (!session.platformAccessToken) {
      localStorage.removeItem(SESSION_STORAGE_KEY)
      return null
    }
    return session
  } catch {
    localStorage.removeItem(SESSION_STORAGE_KEY)
    return null
  }
}

function persist(session: AuthSessionStorage): AuthSessionStorage {
  if (isBrowser()) {
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session))
  }
  return session
}

function readClaimsFromOidcTokens(tokens: OidcTokenResponse): AccessTokenClaims | null {
  const accessClaims = tokens.access_token
    ? tryParseJwtPayload<AccessTokenClaims>(tokens.access_token)
    : null
  const idClaims = tokens.id_token
    ? tryParseJwtPayload<AccessTokenClaims>(tokens.id_token)
    : null

  if (!accessClaims && !idClaims) {
    return null
  }

  const accessPlatformRoles = normalizeRoles(accessClaims?.prole)

  return {
    ...idClaims,
    ...accessClaims,
    prole: accessPlatformRoles.length > 0 ? accessClaims?.prole : idClaims?.prole,
    email: accessClaims?.email ?? idClaims?.email,
    sub: accessClaims?.sub ?? idClaims?.sub,
    uid: accessClaims?.uid ?? idClaims?.uid,
  }
}

export function saveSessionFromOidcTokens(
  tokens: OidcTokenResponse,
  tenants: AuthTenantSummary[] = [],
): AuthSessionStorage {
  const claims = readClaimsFromOidcTokens(tokens)
  const session: AuthSessionStorage = {
    platformAccessToken: tokens.access_token,
    tenantAccessToken: null,
    refreshToken: tokens.refresh_token,
    expiresAtIso: tokenExpiresAtIso(tokens.expires_in),
    tenantExpiresAtIso: null,
    userId: String(claims?.uid ?? claims?.sub ?? ''),
    email: String(claims?.email ?? ''),
    tenantId: null,
    membershipId: null,
    tenantRoles: [],
    platformRoles: normalizeRoles(claims?.prole),
    tenants,
  }
  return persist(session)
}

export function enrichSessionFromUser(
  user: { id: string; email: string; memberships?: { tenantId: string; tenantName: string; tenantKey: string; roles: string[] }[] },
): AuthSessionStorage {
  const current = getAuthSession()
  if (!current) {
    throw new Error('Sessão não encontrada.')
  }

  return persist({
    ...current,
    userId: user.id,
    email: user.email,
    tenants:
      user.memberships?.map((m) => ({
        tenantId: m.tenantId,
        tenantName: m.tenantName,
        tenantKey: m.tenantKey,
        roles: m.roles,
      })) ?? current.tenants,
  })
}

export function applyTenantContext(context: TenantContextResult): AuthSessionStorage {
  const current = getAuthSession()
  if (!current) {
    throw new Error('Sessão não encontrada.')
  }

  if (!context.accessToken) {
    throw new Error('Resposta de switch-tenant sem accessToken (tenant JWT).')
  }

  return persist({
    ...current,
    tenantAccessToken: context.accessToken,
    tenantExpiresAtIso:
      typeof context.expiresIn === 'number'
        ? tokenExpiresAtIso(context.expiresIn)
        : current.tenantExpiresAtIso ?? null,
    userId: context.userId,
    email: context.email,
    tenantId: context.tenantId,
    membershipId: context.membershipId,
    tenantRoles: context.tenantRoles,
    platformRoles: context.platformRoles,
    tenants: context.tenants,
  })
}

/**
 * Refresh updates the platform OIDC token and clears the tenant JWT.
 * Call switch-tenant again when a tenant remains selected.
 */
export function updateSessionFromOidcRefresh(tokens: OidcTokenResponse): AuthSessionStorage {
  const current = getAuthSession()
  const claims = readClaimsFromOidcTokens(tokens)
  const updated: AuthSessionStorage = {
    platformAccessToken: tokens.access_token,
    tenantAccessToken: null,
    refreshToken: tokens.refresh_token,
    expiresAtIso: tokenExpiresAtIso(tokens.expires_in),
    tenantExpiresAtIso: null,
    userId: String(claims?.uid ?? claims?.sub ?? current?.userId ?? ''),
    email: String(claims?.email ?? current?.email ?? ''),
    tenantId: current?.tenantId ?? null,
    membershipId: current?.membershipId ?? null,
    tenantRoles: current?.tenantRoles ?? [],
    platformRoles: normalizeRoles(claims?.prole).length > 0
      ? normalizeRoles(claims?.prole)
      : current?.platformRoles ?? [],
    tenants: current?.tenants ?? [],
  }
  return persist(updated)
}

export function clearAuthSession(): void {
  if (isBrowser()) {
    localStorage.removeItem(SESSION_STORAGE_KEY)
  }
}

export const ACCESS_DENIED_LOGOUT_SESSION_KEY = 'kyvo.auth.access_denied.logout_done'
export const ACCESS_DENIED_MESSAGE_SESSION_KEY = 'kyvo.auth.access_denied.message'

export function stageAccessDeniedLoginMessage(description?: string): void {
  if (!isBrowser()) {
    return
  }

  sessionStorage.setItem(
    ACCESS_DENIED_MESSAGE_SESSION_KEY,
    description ?? PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE,
  )
}

export function consumeAccessDeniedLoginMessage(): string | null {
  if (!isBrowser()) {
    return null
  }

  const message = sessionStorage.getItem(ACCESS_DENIED_MESSAGE_SESSION_KEY)
  if (message) {
    sessionStorage.removeItem(ACCESS_DENIED_MESSAGE_SESSION_KEY)
  }

  return message
}

export function clearAccessDeniedLogoutFlag(): void {
  if (isBrowser()) {
    sessionStorage.removeItem(ACCESS_DENIED_LOGOUT_SESSION_KEY)
    sessionStorage.removeItem(ACCESS_DENIED_MESSAGE_SESSION_KEY)
  }
}

export function isAuthenticated(): boolean {
  const session = getAuthSession()
  return Boolean(session?.platformAccessToken)
}

export function isPlatformAdministrator(session: AuthSessionStorage | null = getAuthSession()): boolean {
  return session?.platformRoles?.includes('plat_admin') ?? false
}

/** Paths that must use the platform OIDC JWT (never the tenant JWT). */
export function requiresPlatformAccessToken(url: string | undefined): boolean {
  if (!url) {
    return false
  }

  const path = url.split('?')[0]
  return (
    path.includes('/auth/switch-tenant') ||
    path.includes('/Applications') ||
    path.includes('/IdentityProviders') ||
    path.includes('/platform/')
  )
}

export function resolveBearerToken(
  session: AuthSessionStorage,
  url?: string,
): string {
  if (requiresPlatformAccessToken(url)) {
    return session.platformAccessToken
  }

  return session.tenantAccessToken || session.platformAccessToken
}

export const PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE =
  'Sua conta não tem permissão para acessar o console da plataforma. Apenas administradores da plataforma podem entrar.'
