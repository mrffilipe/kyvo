import { buildLogoutUrl, clearOidcLoginRequest } from '../services/oidcService'
import {
  ACCESS_DENIED_LOGOUT_SESSION_KEY,
  clearAuthSession,
  PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE,
  stageAccessDeniedLoginMessage,
} from './authStorage'
import { setSelectedTenantId } from './tenantStorage'

export function clearClientAuthState(): void {
  clearAuthSession()
  clearOidcLoginRequest()
  setSelectedTenantId(null)
}

export { clearAccessDeniedLogoutFlag } from './authStorage'

function hasCompletedAccessDeniedCookieLogout(): boolean {
  return sessionStorage.getItem(ACCESS_DENIED_LOGOUT_SESSION_KEY) === '1'
}

function markAccessDeniedCookieLogoutDone(): void {
  sessionStorage.setItem(ACCESS_DENIED_LOGOUT_SESSION_KEY, '1')
}

/** Registered post_logout_redirect_uri for platform-admin-web (no query string). */
export function buildLoginRedirectUri(): string {
  return `${window.location.origin}/login`
}

function parseAccessDeniedDescription(loginUrlWithError: string): string {
  try {
    const url = new URL(loginUrlWithError)
    return url.searchParams.get('error_description') ?? PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE
  } catch {
    return PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE
  }
}

/**
 * Limpa estado local do SPA e encerra a sessão do browser no Kyvo (/connect/logout).
 * Use após falha de login no console Platform Admin (access_denied ou falta de plat_admin).
 */
export function completeFailedPlatformLoginCleanup(description?: string): void {
  clearClientAuthState()
  markAccessDeniedCookieLogoutDone()
  stageAccessDeniedLoginMessage(description)
  window.location.replace(buildLogoutUrl(buildLoginRedirectUri()))
}

/**
 * Garante logout do cookie Kyvo quando o usuário chega em /login?error=access_denied
 * sem passar pelo callback (ex.: loader de rota protegida).
 */
export function ensureAccessDeniedCookieLogout(loginUrlWithError: string): boolean {
  if (hasCompletedAccessDeniedCookieLogout()) {
    return false
  }

  markAccessDeniedCookieLogoutDone()
  clearClientAuthState()
  stageAccessDeniedLoginMessage(parseAccessDeniedDescription(loginUrlWithError))
  window.location.replace(buildLogoutUrl(buildLoginRedirectUri()))
  return true
}
