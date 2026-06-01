import { buildLogoutUrl, clearOidcLoginRequest } from '../services/oidcService'
import {
  ACCESS_DENIED_LOGOUT_SESSION_KEY,
  clearAuthSession,
  PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE,
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

export function buildLoginUrlWithAccessDenied(description?: string): string {
  const url = new URL(`${window.location.origin}/login`)
  url.searchParams.set('error', 'access_denied')
  url.searchParams.set('error_description', description ?? PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE)
  return url.toString()
}

/**
 * Limpa estado local do SPA e encerra a sessão do browser no Kyvo (/connect/logout).
 * Use após falha de login no console Platform Admin (access_denied ou falta de plat_admin).
 */
export function completeFailedPlatformLoginCleanup(description?: string): void {
  clearClientAuthState()
  markAccessDeniedCookieLogoutDone()
  window.location.replace(buildLogoutUrl(buildLoginUrlWithAccessDenied(description)))
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
  window.location.replace(buildLogoutUrl(loginUrlWithError))
  return true
}
