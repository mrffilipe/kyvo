import AddIcon from '@mui/icons-material/Add'
import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import DeleteIcon from '@mui/icons-material/Delete'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import { Button, IconButton, MenuItem, Stack, TableCell, TableRow, TextField, Tooltip, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import {
  DataTable,
  FeedbackAlerts,
  FormGrid,
  FormGridItem,
  FormSection,
  PageHeader,
  PaginatedAutocomplete,
  ResourceDialog,
  SectionCard,
  StaticField,
  StatusChip,
  TenantScopeNotice,
} from '../components/ui'
import { useTenant } from '../contexts/TenantContext'
import { useTenantRoleOptions } from '../hooks/useTenantRoleOptions'
import {
  createMembership,
  listInvitesByTenant,
  listMembershipsByTenant,
  revokeInvite,
  revokeMembership,
  searchUsers,
  updateMembershipRole,
} from '../services'
import type { Membership, TenantInvite, UserPickerItem } from '../types'
import { getApiErrorMessage } from '../utils/apiError'
import { tenantInviteStatusLabel, tenantRoleLabel } from '../utils/enumLabels'
import { copyInviteLink } from '../utils/inviteUrl'

export function MembershipsPage() {
  const { tenantId } = useTenant()
  const [items, setItems] = useState<Membership[]>([])
  const [invites, setInvites] = useState<TenantInvite[]>([])
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [createOpen, setCreateOpen] = useState(false)
  const [editOpen, setEditOpen] = useState(false)
  const [loading, setLoading] = useState(false)

  const [selectedUser, setSelectedUser] = useState<UserPickerItem | null>(null)
  const [createRole, setCreateRole] = useState('member')

  const [editingMembership, setEditingMembership] = useState<Membership | null>(null)
  const [updateRole, setUpdateRole] = useState('viewer')

  const { roleKeys, loading: rolesLoading } = useTenantRoleOptions(tenantId)

  useEffect(() => {
    if (roleKeys.length > 0) {
      if (!roleKeys.includes(createRole)) {
        setCreateRole(roleKeys.includes('member') ? 'member' : roleKeys[0])
      }
      if (!roleKeys.includes(updateRole)) {
        setUpdateRole(roleKeys[0])
      }
    }
  }, [roleKeys, createRole, updateRole])

  useEffect(() => {
    if (!tenantId) {
      setItems([])
      setInvites([])
      return
    }
    void loadMemberships(tenantId)
    void loadInvites(tenantId)
  }, [tenantId])

  async function loadMemberships(currentTenantId: string): Promise<void> {
    setError(null)
    try {
      const result = await listMembershipsByTenant(currentTenantId, { page: 1, pageSize: 100 })
      setItems(result.items)
    } catch (loadError) {
      setError(getApiErrorMessage(loadError))
    }
  }

  async function loadInvites(currentTenantId: string): Promise<void> {
    try {
      const result = await listInvitesByTenant(currentTenantId, { page: 1, pageSize: 100 })
      setInvites(result.items)
    } catch (loadError) {
      setError(getApiErrorMessage(loadError))
    }
  }

  function openCreateDialog(): void {
    setSelectedUser(null)
    setCreateRole(roleKeys.includes('member') ? 'member' : (roleKeys[0] ?? 'member'))
    setCreateOpen(true)
  }

  function openEditDialog(membership: Membership): void {
    setEditingMembership(membership)
    setUpdateRole(membership.roles[0] ?? roleKeys[0] ?? 'viewer')
    setEditOpen(true)
  }

  async function handleCreate(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    if (!tenantId) {
      setError('Selecione um tenant primeiro na tela de Tenants.')
      return
    }
    if (!selectedUser) {
      setError('Selecione um usuário na busca.')
      return
    }

    setLoading(true)
    setError(null)
    setSuccess(null)
    try {
      const created = await createMembership(tenantId, { userId: selectedUser.id, roles: [createRole] })
      setSuccess(`Membro vinculado: ${created.id}`)
      setCreateOpen(false)
      await loadMemberships(tenantId)
    } catch (createError) {
      setError(getApiErrorMessage(createError))
    } finally {
      setLoading(false)
    }
  }

  async function handleUpdate(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    if (!editingMembership) {
      return
    }
    setLoading(true)
    setError(null)
    setSuccess(null)
    try {
      await updateMembershipRole(editingMembership.id, { roles: [updateRole] })
      setSuccess('Papel do membro atualizado.')
      setEditOpen(false)
      if (tenantId) {
        await loadMemberships(tenantId)
      }
    } catch (updateError) {
      setError(getApiErrorMessage(updateError))
    } finally {
      setLoading(false)
    }
  }

  async function handleDelete(id: string): Promise<void> {
    setError(null)
    setSuccess(null)
    try {
      await revokeMembership(id)
      setSuccess('Membro revogado.')
      if (tenantId) {
        await loadMemberships(tenantId)
      }
    } catch (deleteError) {
      setError(getApiErrorMessage(deleteError))
    }
  }

  async function handleCopyInviteLink(acceptPath: string): Promise<void> {
    setError(null)
    try {
      await copyInviteLink(acceptPath)
      setSuccess('Link do convite copiado.')
    } catch (copyError) {
      setError(getApiErrorMessage(copyError))
    }
  }

  async function handleRevokeInvite(inviteId: string): Promise<void> {
    setError(null)
    setSuccess(null)
    try {
      await revokeInvite(inviteId)
      setSuccess('Convite revogado.')
      if (tenantId) {
        await loadInvites(tenantId)
      }
    } catch (revokeError) {
      setError(getApiErrorMessage(revokeError))
    }
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Membros"
        description="Membros e papéis vinculados ao tenant selecionado."
        actions={
          <Button startIcon={<AddIcon />} onClick={openCreateDialog} disabled={!tenantId}>
            Novo membro
          </Button>
        }
      />
      <FeedbackAlerts success={success} error={error} />
      <TenantScopeNotice />

      <SectionCard title="Convites pendentes">
        <DataTable
          columns={[
            { id: 'email', label: 'E-mail' },
            { id: 'roles', label: 'Papéis' },
            { id: 'expires', label: 'Expira em' },
            { id: 'status', label: 'Status' },
            { id: 'actions', label: 'Ações', align: 'right' },
          ]}
          rows={invites.map((invite) => (
            <TableRow key={invite.id} hover>
              <TableCell>{invite.email}</TableCell>
              <TableCell>{invite.roles.map(tenantRoleLabel).join(', ')}</TableCell>
              <TableCell>{new Date(invite.expiresAt).toLocaleString()}</TableCell>
              <TableCell>
                <StatusChip
                  label={tenantInviteStatusLabel(invite.status)}
                  variant={invite.status === 'Pending' ? 'warning' : 'default'}
                />
              </TableCell>
              <TableCell align="right">
                {invite.acceptPath ? (
                  <Tooltip title="Copiar link do convite">
                    <IconButton size="small" onClick={() => void handleCopyInviteLink(invite.acceptPath!)}>
                      <ContentCopyIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                ) : null}
                {invite.status === 'Pending' ? (
                  <Tooltip title="Revogar convite">
                    <IconButton color="error" size="small" onClick={() => void handleRevokeInvite(invite.id)}>
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                ) : null}
              </TableCell>
            </TableRow>
          ))}
          emptyDescription={tenantId ? 'Nenhum convite para este tenant.' : 'Selecione um tenant em Tenants.'}
        />
      </SectionCard>

      <SectionCard title="Membros do tenant">
        <DataTable
          columns={[
            { id: 'member', label: 'Membro' },
            { id: 'roles', label: 'Papéis' },
            { id: 'active', label: 'Ativo' },
            { id: 'actions', label: 'Ações', align: 'right' },
          ]}
          rows={items.map((item) => (
            <TableRow key={item.id} hover>
              <TableCell>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {item.userDisplayName ?? '—'}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {item.userEmail ?? item.userId}
                </Typography>
              </TableCell>
              <TableCell>{item.roles.map(tenantRoleLabel).join(', ')}</TableCell>
              <TableCell>
                <StatusChip label={item.isActive ? 'Ativo' : 'Inativo'} variant={item.isActive ? 'success' : 'default'} />
              </TableCell>
              <TableCell align="right">
                <Tooltip title="Editar papel">
                  <IconButton size="small" onClick={() => openEditDialog(item)}>
                    <EditOutlinedIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Revogar membro">
                  <IconButton color="error" size="small" onClick={() => void handleDelete(item.id)}>
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              </TableCell>
            </TableRow>
          ))}
          emptyDescription={tenantId ? 'Nenhum membro neste tenant.' : 'Selecione um tenant em Tenants.'}
        />
      </SectionCard>

      <ResourceDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title="Novo membro"
        description="Vincule um usuário ao tenant ativo com um papel inicial."
        loading={loading}
        submitLabel="Criar"
        onSubmit={handleCreate}
        disableSubmit={!tenantId || !selectedUser}
      >
        <FormSection title="Membro" description="Busque o usuário por nome ou e-mail.">
          <FormGrid>
            <FormGridItem xs={12} md={12}>
              <PaginatedAutocomplete
                label="Usuário"
                value={selectedUser}
                onChange={setSelectedUser}
                fetchPage={(query, page) => searchUsers({ search: query, page, pageSize: 20 })}
                getOptionLabel={(option) => `${option.displayName} (${option.email})`}
                isOptionEqualToValue={(a, b) => a.id === b.id}
              />
            </FormGridItem>
            <FormGridItem>
              <TextField
                select
                label="Papel"
                value={createRole}
                onChange={(e) => setCreateRole(e.target.value)}
                fullWidth
                disabled={rolesLoading || roleKeys.length === 0}
              >
                {roleKeys.map((role) => (
                  <MenuItem key={role} value={role}>
                    {tenantRoleLabel(role)}
                  </MenuItem>
                ))}
              </TextField>
            </FormGridItem>
          </FormGrid>
        </FormSection>
      </ResourceDialog>

      <ResourceDialog
        open={editOpen}
        onClose={() => setEditOpen(false)}
        title="Atualizar membro"
        description={editingMembership ? editingMembership.userEmail ?? editingMembership.userId : undefined}
        loading={loading}
        submitLabel="Salvar"
        onSubmit={handleUpdate}
      >
        <FormSection title="Papel" description="Altere o papel principal deste membro.">
          <FormGrid>
            <FormGridItem xs={12} md={12}>
              <StaticField
                label="Usuário"
                value={
                  editingMembership?.userDisplayName
                    ? `${editingMembership.userDisplayName} (${editingMembership.userEmail ?? editingMembership.userId})`
                    : (editingMembership?.userEmail ?? editingMembership?.userId)
                }
              />
            </FormGridItem>
            <FormGridItem>
              <TextField
                select
                label="Papel"
                value={updateRole}
                onChange={(e) => setUpdateRole(e.target.value)}
                fullWidth
                disabled={rolesLoading || roleKeys.length === 0}
              >
                {roleKeys.map((role) => (
                  <MenuItem key={role} value={role}>
                    {tenantRoleLabel(role)}
                  </MenuItem>
                ))}
              </TextField>
            </FormGridItem>
          </FormGrid>
        </FormSection>
      </ResourceDialog>
    </Stack>
  )
}
