import type { HttpClient } from '../api/httpClient.js'
import type { ApiPaths } from '../api/paths.js'
import type { AuditLogItemDto, ListAuditLogsFilters, PagedResult } from '../types/api.js'

export function createAuditLogsResource(http: HttpClient, paths: ApiPaths) {
  return {
    list(filters: ListAuditLogsFilters = {}): Promise<PagedResult<AuditLogItemDto>> {
      return http.request('GET', paths.auditLogs, {
        params: {
          page: filters.page ?? 1,
          pageSize: filters.pageSize ?? 20,
          userId: filters.userId,
          action: filters.action,
          resourceType: filters.resourceType,
          from: filters.from,
          to: filters.to,
        },
      })
    },
  }
}
