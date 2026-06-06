import type { PagedResult } from './common'

export interface Tenant {
  id: string
  name: string
  key: string
}

export interface CreateTenantBody {
  name: string
  key: string
  initialAdministratorUserId?: string | null
  initialAdministratorEmail?: string | null
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

export type TenantInviteStatus = 'Pending' | 'Accepted' | 'Expired' | 'Revoked'

export interface TenantInvite {
  id: string
  email: string
  roles: string[]
  expiresAt: string
  consumedAt?: string | null
  revokedAt?: string | null
  status: TenantInviteStatus
  acceptPath?: string | null
}

export interface InviteMemberResponse {
  id: string
  acceptPath: string
}

export interface AcceptInviteResponse {
  membershipId: string
}

export type ListTenantsResponse = PagedResult<Tenant>

export type ListTenantInvitesResponse = PagedResult<TenantInvite>
