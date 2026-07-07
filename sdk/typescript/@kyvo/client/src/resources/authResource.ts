import type { HttpClient } from '../api/httpClient.js'
import type { ApiPaths } from '../api/paths.js'
import type { AuthSessionDto, TenantContextResult } from '../types/api.js'
import type { SessionManager } from '../session/sessionManager.js'

export function createAuthResource(http: HttpClient, paths: ApiPaths, session: SessionManager) {
  return {
    async switchTenant(tenantId: string): Promise<TenantContextResult> {
      const result = await http.requestWithPlatformToken<TenantContextResult>(
        'POST',
        `${paths.auth}/switch-tenant`,
        { body: { tenantId } },
      )

      const token = result.accessToken
      if (token) {
        session.saveTenantToken(
          token,
          result.expiresIn ?? 900,
          result.tenantId,
          result.membershipId,
        )
      }

      return result
    },

    listSessions(): Promise<AuthSessionDto[]> {
      return http.request('GET', `${paths.auth}/sessions`)
    },

    revokeSession(sessionId: string): Promise<void> {
      return http.request('DELETE', `${paths.auth}/sessions/${sessionId}`)
    },

    deleteAccount(): Promise<void> {
      return http.request('DELETE', `${paths.auth}/account`)
    },
  }
}

// subscribe intentionally omitted — BFF / .NET only
