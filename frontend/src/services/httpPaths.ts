const versionPrefix = '/api/v1'

/** Paths aligned with Kyvo REST API `/api/v1/*`. */
export const apiPaths = {
  versionPrefix,
  auth: `${versionPrefix}/auth`,
  platform: `${versionPrefix}/platform`,
  users: `${versionPrefix}/Users`,
  tenants: `${versionPrefix}/Tenants`,
  memberships: `${versionPrefix}/Memberships`,
  tenantRoles: `${versionPrefix}/TenantRoles`,
  applications: `${versionPrefix}/Applications`,
  auditLogs: `${versionPrefix}/AuditLogs`,
  identityProviders: `${versionPrefix}/IdentityProviders`,
  invites: `${versionPrefix}/invites`,
  wellKnown: '/.well-known',
  connectAuthorize: '/connect/authorize',
  connectToken: '/connect/token',
  connectLogout: '/connect/logout',
  connectUserinfo: '/connect/userinfo',
  accountLogin: '/account/login',
  accountLogout: '/account/logout',
} as const
