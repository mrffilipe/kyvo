import { api } from '../config'
import type {
  ListAuditFilterOptionsResponse,
  ListAuditLogsFilters,
  ListAuditLogsResponse,
} from '../types'
import { normalizeAuditFilterOption, normalizeAuditLogItem } from '../utils/apiMappers'
import { unwrapPagedResult } from '../utils/apiResponse'
import { compactQuery } from '../utils/queryParams'
import { apiPaths } from './httpPaths'

export async function listAuditLogs(filters: ListAuditLogsFilters = {}): Promise<ListAuditLogsResponse> {
  const { data } = await api.get(apiPaths.auditLogs, {
    params: compactQuery({
      ...filters,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20,
    }),
  })
  return unwrapPagedResult(data, normalizeAuditLogItem)
}

export interface ListAuditFilterOptionsParams {
  field: 'action' | 'resourceType'
  search?: string
  page?: number
  pageSize?: number
}

export async function listAuditFilterOptions(
  params: ListAuditFilterOptionsParams,
): Promise<ListAuditFilterOptionsResponse> {
  const { data } = await api.get(`${apiPaths.auditLogs}/filter-options`, {
    params: compactQuery({
      field: params.field,
      search: params.search || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }),
  })
  return unwrapPagedResult(data, normalizeAuditFilterOption)
}
