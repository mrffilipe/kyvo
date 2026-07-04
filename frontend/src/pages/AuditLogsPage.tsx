import { Autocomplete, Button, Stack, TableCell, TableRow, TextField, Tooltip, Typography } from '@mui/material'
import { DateTimePicker } from '@mui/x-date-pickers/DateTimePicker'
import type { Dayjs } from 'dayjs'
import { useMemo, useState } from 'react'
import { AUDIT_ACTION_OPTIONS, AUDIT_RESOURCE_TYPE_OPTIONS } from '../constants/auditLogFilters'
import {
  DataTable,
  FeedbackAlerts,
  FormActions,
  FormGrid,
  FormGridItem,
  PageHeader,
  PaginatedAutocomplete,
  SectionCard,
} from '../components/ui'
import { listAuditLogs, searchUsers } from '../services'
import type { AuditLogItem, UserPickerItem } from '../types'
import { getApiErrorMessage } from '../utils/apiError'

type AuditFilterOption = { value: string; label: string }

export function AuditLogsPage() {
  const [items, setItems] = useState<AuditLogItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [actionFilter, setActionFilter] = useState<AuditFilterOption | null>(null)
  const [resourceTypeFilter, setResourceTypeFilter] = useState<AuditFilterOption | null>(null)
  const [userFilter, setUserFilter] = useState<UserPickerItem | null>(null)
  const [from, setFrom] = useState<Dayjs | null>(null)
  const [to, setTo] = useState<Dayjs | null>(null)

  const actionOptions = useMemo(() => [...AUDIT_ACTION_OPTIONS], [])
  const resourceTypeOptions = useMemo(() => [...AUDIT_RESOURCE_TYPE_OPTIONS], [])

  async function handleSearch(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    setError(null)
    try {
      const data = await listAuditLogs({
        action: actionFilter?.value,
        resourceType: resourceTypeFilter?.value,
        userId: userFilter?.id,
        from: from ? from.toISOString() : undefined,
        to: to ? to.toISOString() : undefined,
        page: 1,
        pageSize: 100,
      })
      setItems(data.items)
    } catch (searchError) {
      setError(getApiErrorMessage(searchError))
    }
  }

  return (
    <Stack spacing={3}>
      <PageHeader title="Logs de auditoria" description="Consulte o histórico de ações registradas na plataforma." />
      <FeedbackAlerts error={error} />

      <SectionCard title="Filtros de busca">
        <Stack component="form" onSubmit={handleSearch} spacing={2}>
          <FormGrid>
            <FormGridItem>
              <Autocomplete
                options={actionOptions}
                value={actionFilter}
                onChange={(_event, value) => setActionFilter(value)}
                getOptionLabel={(option) => option.label}
                isOptionEqualToValue={(a, b) => a.value === b.value}
                renderInput={(params) => <TextField {...params} label="Ação" fullWidth />}
              />
            </FormGridItem>
            <FormGridItem>
              <Autocomplete
                options={resourceTypeOptions}
                value={resourceTypeFilter}
                onChange={(_event, value) => setResourceTypeFilter(value)}
                getOptionLabel={(option) => option.label}
                isOptionEqualToValue={(a, b) => a.value === b.value}
                renderInput={(params) => <TextField {...params} label="Tipo de recurso" fullWidth />}
              />
            </FormGridItem>
            <FormGridItem>
              <PaginatedAutocomplete
                label="Usuário"
                value={userFilter}
                onChange={setUserFilter}
                fetchPage={(query, page) => searchUsers({ search: query, page, pageSize: 20 })}
                getOptionLabel={(option) => `${option.displayName} (${option.email})`}
                isOptionEqualToValue={(a, b) => a.id === b.id}
              />
            </FormGridItem>
            <FormGridItem>
              <DateTimePicker
                label="De"
                value={from}
                onChange={setFrom}
                slotProps={{ textField: { fullWidth: true } }}
              />
            </FormGridItem>
            <FormGridItem>
              <DateTimePicker
                label="Até"
                value={to}
                onChange={setTo}
                minDateTime={from ?? undefined}
                slotProps={{ textField: { fullWidth: true } }}
              />
            </FormGridItem>
          </FormGrid>
          <FormActions>
            <Button type="submit">Buscar logs</Button>
          </FormActions>
        </Stack>
      </SectionCard>

      <SectionCard title="Resultados">
        <DataTable
          columns={[
            { id: 'id', label: 'Id', minWidth: 120 },
            { id: 'tenantId', label: 'Tenant', minWidth: 120 },
            { id: 'userId', label: 'Usuário', minWidth: 120 },
            { id: 'action', label: 'Ação' },
            { id: 'resourceType', label: 'Recurso' },
            { id: 'createdAt', label: 'Criado em' },
          ]}
          rows={items.map((item) => (
            <TableRow key={item.id} hover>
              <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>{item.id}</TableCell>
              <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                <Tooltip title={item.tenantId}>
                  <Typography variant="body2" noWrap sx={{ maxWidth: 120 }}>
                    {item.tenantId}
                  </Typography>
                </Tooltip>
              </TableCell>
              <TableCell>{item.userId ?? '-'}</TableCell>
              <TableCell>{item.action}</TableCell>
              <TableCell>{item.resourceType}</TableCell>
              <TableCell>{new Date(item.createdAt).toLocaleString('pt-BR')}</TableCell>
            </TableRow>
          ))}
          emptyDescription="Use os filtros acima para buscar registros de auditoria."
        />
      </SectionCard>
    </Stack>
  )
}
