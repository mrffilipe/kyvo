import type { PagedResult } from './common'

export interface AuditLogItem {
  id: string
  tenantId: string
  userId?: string | null
  membershipId?: string | null
  action: string
  resourceType: string
  resourceId?: string | null
  ipAddress?: string | null
  userAgent?: string | null
  createdAt: string
}

export interface AuditLogFilterOption {
  value: string
}

export interface ListAuditLogsFilters {
  action?: string
  resourceType?: string
  userId?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

export type ListAuditLogsResponse = PagedResult<AuditLogItem>
export type ListAuditFilterOptionsResponse = PagedResult<AuditLogFilterOption>
