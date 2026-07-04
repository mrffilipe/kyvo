import { useEffect, useState } from 'react'
import { listTenantRoles } from '../services'
import type { TenantRole } from '../types'
import { getApiErrorMessage } from '../utils/apiError'

export function useTenantRoleOptions(tenantId: string | null): {
  roles: TenantRole[]
  roleKeys: string[]
  loading: boolean
  error: string | null
} {
  const [roles, setRoles] = useState<TenantRole[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!tenantId) {
      setRoles([])
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    void listTenantRoles(tenantId, { includeInactive: false, page: 1, pageSize: 100 })
      .then((result) => {
        if (!cancelled) {
          setRoles(result.items.filter((role) => role.isActive))
        }
      })
      .catch((loadError) => {
        if (!cancelled) {
          setError(getApiErrorMessage(loadError))
          setRoles([])
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [tenantId])

  return {
    roles,
    roleKeys: roles.map((role) => role.key),
    loading,
    error,
  }
}
