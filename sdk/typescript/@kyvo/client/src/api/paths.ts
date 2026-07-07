/** Product REST API paths under `/api/v1/*`. */
export function createApiPaths() {
  const versionPrefix = '/api/v1'
  return {
    versionPrefix,
    auth: `${versionPrefix}/auth`,
    users: `${versionPrefix}/Users`,
    tenants: `${versionPrefix}/Tenants`,
    memberships: `${versionPrefix}/Memberships`,
    tenantRoles: `${versionPrefix}/TenantRoles`,
    auditLogs: `${versionPrefix}/AuditLogs`,
    invites: `${versionPrefix}/invites`,
    connectAuthorize: '/connect/authorize',
    connectToken: '/connect/token',
    connectLogout: '/connect/logout',
    connectUserinfo: '/connect/userinfo',
  } as const
}

export type ApiPaths = ReturnType<typeof createApiPaths>
