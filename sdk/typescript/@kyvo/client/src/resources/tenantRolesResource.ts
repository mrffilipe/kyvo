import type { HttpClient } from '../api/httpClient.js'
import type { ApiPaths } from '../api/paths.js'
import type {
  CreateTenantRoleBody,
  CreatedIdResponse,
  PagedResult,
  TenantRoleDto,
  UpdateTenantRoleBody,
} from '../types/api.js'

export function createTenantRolesResource(http: HttpClient, paths: ApiPaths) {
  return {
    list(
      tenantId: string,
      includeInactive = false,
      page = 1,
      pageSize = 20,
    ): Promise<PagedResult<TenantRoleDto>> {
      return http.request('GET', `${paths.versionPrefix}/tenants/${tenantId}/roles`, {
        params: { includeInactive, page, pageSize },
      })
    },

    create(tenantId: string, body: CreateTenantRoleBody): Promise<CreatedIdResponse> {
      return http.request('POST', `${paths.versionPrefix}/tenants/${tenantId}/roles`, { body })
    },

    update(roleId: string, body: UpdateTenantRoleBody): Promise<void> {
      return http.request('PATCH', `${paths.tenantRoles}/${roleId}`, { body })
    },

    delete(roleId: string): Promise<void> {
      return http.request('DELETE', `${paths.tenantRoles}/${roleId}`)
    },
  }
}
