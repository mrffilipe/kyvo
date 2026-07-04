import { api } from '../config'
import { normalizeTenant } from '../utils/apiMappers'
import { unwrapPagedResult } from '../utils/apiResponse'
import type {
  AcceptInviteBody,
  AcceptInviteResponse,
  CreateTenantBody,
  InviteMemberBody,
  InviteMemberResponse,
  ListTenantInvitesResponse,
  ListTenantsResponse,
  Tenant,
  TenantInvite,
  UpdateTenantBody,
} from '../types'
import { apiPaths } from './httpPaths'

export interface ListTenantsParams {
  page?: number
  pageSize?: number
  search?: string
}

export async function createTenant(body: CreateTenantBody): Promise<{ id: string }> {
  const { data } = await api.post<{ id: string }>(apiPaths.tenants, body)
  return data
}

export async function listTenants(params: ListTenantsParams = {}): Promise<ListTenantsResponse> {
  const { data } = await api.get(apiPaths.tenants, {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      search: params.search || undefined,
    },
  })
  return unwrapPagedResult(data, normalizeTenant)
}

export async function checkTenantKeyAvailability(key: string): Promise<boolean> {
  const encoded = encodeURIComponent(key.trim().toLowerCase())
  const { data } = await api.get<{ available: boolean }>(`${apiPaths.tenants}/keys/${encoded}/availability`)
  return data.available
}

export async function getTenantById(id: string): Promise<Tenant> {
  const { data } = await api.get(`${apiPaths.tenants}/${id}`)
  return normalizeTenant(data)
}

export async function updateTenant(id: string, body: UpdateTenantBody): Promise<void> {
  await api.patch(`${apiPaths.tenants}/${id}`, body)
}

export async function inviteMember(id: string, body: InviteMemberBody): Promise<InviteMemberResponse> {
  const { data } = await api.post<InviteMemberResponse>(`${apiPaths.tenants}/${id}/invites`, body)
  return data
}

export interface ListTenantInvitesParams {
  page?: number
  pageSize?: number
}

function normalizeTenantInvite(item: unknown): TenantInvite {
  const raw = item as Record<string, unknown>
  return {
    id: String(raw.id),
    email: String(raw.email ?? ''),
    roles: Array.isArray(raw.roles) ? raw.roles.map(String) : [],
    expiresAt: String(raw.expiresAt),
    consumedAt: raw.consumedAt != null ? String(raw.consumedAt) : null,
    revokedAt: raw.revokedAt != null ? String(raw.revokedAt) : null,
    status: String(raw.status) as TenantInvite['status'],
    acceptPath: raw.acceptPath != null ? String(raw.acceptPath) : null,
  }
}

export async function listInvitesByTenant(
  tenantId: string,
  params: ListTenantInvitesParams = {},
): Promise<ListTenantInvitesResponse> {
  const { data } = await api.get(`${apiPaths.tenants}/${tenantId}/invites`, {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return unwrapPagedResult(data, normalizeTenantInvite)
}

export async function revokeInvite(inviteId: string): Promise<void> {
  await api.delete(`${apiPaths.invites}/${inviteId}`)
}

export async function acceptInvite(body: AcceptInviteBody): Promise<AcceptInviteResponse> {
  const { data } = await api.post<AcceptInviteResponse>(`${apiPaths.invites}/accept`, body)
  return data
}
