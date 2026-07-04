import type { HttpClient } from '../api/httpClient.js'
import type { ApiPaths } from '../api/paths.js'
import type {
  AcceptInviteBody,
  AvailabilityDto,
  CreatedMembershipIdResponse,
  InviteMemberBody,
  InviteMemberResponse,
  PagedResult,
  TenantDto,
  TenantInviteDto,
  UpdateTenantBody,
} from '../types/api.js'

export function createTenantsResource(http: HttpClient, paths: ApiPaths) {
  return {
    list(page = 1, pageSize = 20, search?: string): Promise<PagedResult<TenantDto>> {
      return http.request('GET', paths.tenants, { params: { page, pageSize, search } })
    },

    getById(id: string): Promise<TenantDto> {
      return http.request('GET', `${paths.tenants}/${id}`)
    },

    update(id: string, body: UpdateTenantBody): Promise<void> {
      return http.request('PATCH', `${paths.tenants}/${id}`, { body })
    },

    inviteMember(id: string, body: InviteMemberBody): Promise<InviteMemberResponse> {
      return http.request('POST', `${paths.tenants}/${id}/invites`, { body })
    },

    listInvites(
      id: string,
      page = 1,
      pageSize = 20,
    ): Promise<PagedResult<TenantInviteDto>> {
      return http.request('GET', `${paths.tenants}/${id}/invites`, { params: { page, pageSize } })
    },

    revokeInvite(inviteId: string): Promise<void> {
      return http.request('DELETE', `${paths.invites}/${inviteId}`)
    },

    acceptInvite(body: AcceptInviteBody): Promise<CreatedMembershipIdResponse> {
      return http.request('POST', `${paths.invites}/accept`, { body })
    },

    checkKeyAvailability(key: string): Promise<AvailabilityDto> {
      return http.request('GET', `${paths.tenants}/keys/${encodeURIComponent(key)}/availability`)
    },
  }
}
