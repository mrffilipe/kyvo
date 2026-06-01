/** Product REST v1 DTOs — aligned with Kyvo OpenAPI (`/swagger/v1/swagger.json`). */

export type SessionStatus = 'Active' | 'Revoked' | 'Expired'

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface CreatedIdResponse {
  id: string
}

export interface CreatedMembershipIdResponse {
  membershipId: string
}

export interface TenantContextResult {
  userId: string
  email: string
  tenantId?: string
  membershipId?: string
  tenantRoles: string[]
  platformRoles: string[]
  tenants: AuthTenantSummaryDto[]
}

export interface AuthTenantSummaryDto {
  tenantId: string
  tenantName: string
  tenantKey: string
  roles: string[]
}

export interface AuthSessionDto {
  sessionId: string
  tenantId?: string
  membershipId?: string
  clientId?: string
  status: SessionStatus
  userAgent?: string
  ipAddress?: string
  expiresAt: string
  lastActivityAt: string
}

export interface UserDto {
  id: string
  email: string
  displayName: string
  photoUrl?: string
  memberships: UserMembershipDto[]
}

export interface UserMembershipDto {
  membershipId: string
  tenantId: string
  tenantName: string
  tenantKey: string
  roles: string[]
}

export interface UpdateUserProfileBody {
  displayName: string
  photoUrl?: string
}

export interface TenantDto {
  id: string
  name: string
  key: string
}

export interface UpdateTenantBody {
  name: string
}

export interface InviteMemberBody {
  email: string
  roles: string[]
}

export interface AcceptInviteBody {
  token: string
}

export interface MembershipDto {
  id: string
  userId: string
  userEmail?: string
  userDisplayName?: string
  tenantId: string
  roles: string[]
  isActive: boolean
}

export interface CreateMembershipBody {
  userId: string
  roles: string[]
}

export interface UpdateMembershipRolesBody {
  roles: string[]
}

export interface TenantRoleDto {
  id: string
  tenantId: string
  key: string
  name: string
  description?: string
  isSystem: boolean
  isActive: boolean
}

export interface CreateTenantRoleBody {
  key: string
  name: string
  description?: string
}

export interface UpdateTenantRoleBody {
  name: string
  description?: string
  isActive: boolean
}

export interface AuditLogItemDto {
  id: string
  tenantId: string
  userId?: string
  membershipId?: string
  action: string
  resourceType: string
  resourceId?: string
  ipAddress?: string
  userAgent?: string
  createdAt: string
}

export interface ListAuditLogsFilters {
  page?: number
  pageSize?: number
  userId?: string
  action?: string
  resourceType?: string
  from?: string
  to?: string
}
