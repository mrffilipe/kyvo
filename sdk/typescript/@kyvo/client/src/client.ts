import { createApiPaths } from './api/paths.js'
import { createHttpClientFromSession } from './api/httpClient.js'
import * as claims from './claims/parseClaims.js'
import { OidcClient } from './oidc/oidcClient.js'
import { createAuthResource } from './resources/authResource.js'
import { createUsersResource } from './resources/usersResource.js'
import { createTenantsResource } from './resources/tenantsResource.js'
import { createMembershipsResource } from './resources/membershipsResource.js'
import { createTenantRolesResource } from './resources/tenantRolesResource.js'
import { createAuditLogsResource } from './resources/auditLogsResource.js'
import { SessionManager } from './session/sessionManager.js'
import { createMemoryStorage } from './session/memoryStorage.js'
import type { KyvoClientConfig } from './types.js'
import type { TenantContextResult } from './types/api.js'

const DEFAULT_SCOPES = 'openid profile email offline_access kyvo_api'

export interface KyvoClient {
  oidc: OidcClient
  session: SessionManager
  claims: typeof claims
  auth: ReturnType<typeof createAuthResource>
  users: ReturnType<typeof createUsersResource>
  tenants: ReturnType<typeof createTenantsResource>
  memberships: ReturnType<typeof createMembershipsResource>
  tenantRoles: ReturnType<typeof createTenantRolesResource>
  auditLogs: ReturnType<typeof createAuditLogsResource>

  /** Tenant access token when active; otherwise platform OIDC token. */
  getAccessToken(): string | null

  /**
   * Two-step auth: exchange base OIDC token for a tenant-scoped JWT.
   * Saves the tenant token in session automatically.
   */
  switchTenant(tenantId: string): Promise<TenantContextResult>
}

export function createKyvoClient(config: KyvoClientConfig): KyvoClient {
  const authority = config.authority.replace(/\/$/, '')
  const paths = createApiPaths()

  const sessionStorage = config.oidc.storage ?? (
    typeof globalThis !== 'undefined' && 'localStorage' in globalThis
      ? globalThis.localStorage
      : createMemoryStorage()
  )

  const pkceStorage =
    config.oidc.pkceStorage ??
    (typeof globalThis !== 'undefined' && 'sessionStorage' in globalThis
      ? globalThis.sessionStorage
      : createMemoryStorage())

  const session = new SessionManager(sessionStorage)
  const oidc = new OidcClient({
    authority,
    clientId: config.oidc.clientId,
    redirectUri: config.oidc.redirectUri,
    scopes: config.oidc.scopes ?? DEFAULT_SCOPES,
    pkceStorage,
  })

  const http = createHttpClientFromSession(authority, session, (rt) => oidc.refresh(rt))
  const auth = createAuthResource(http, paths, session)

  const client: KyvoClient = {
    oidc,
    session,
    claims,
    auth,
    users: createUsersResource(http, paths),
    tenants: createTenantsResource(http, paths),
    memberships: createMembershipsResource(http, paths),
    tenantRoles: createTenantRolesResource(http, paths),
    auditLogs: createAuditLogsResource(http, paths),
    getAccessToken: () => session.getAccessToken() ?? session.getPlatformAccessToken(),
    switchTenant: (tenantId) => auth.switchTenant(tenantId),
  }

  return client
}
