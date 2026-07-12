import AddIcon from '@mui/icons-material/Add'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import MailOutlineIcon from '@mui/icons-material/MailOutlineOutlined'
import SearchIcon from '@mui/icons-material/Search'
import {
  Button,
  IconButton,
  MenuItem,
  Stack,
  TableCell,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  AvailabilityTextField,
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
  UserPickerField,
} from '../components/ui'
import { useAuth } from '../contexts/AuthContext'
import { useTenant } from '../contexts/TenantContext'
import { useDebouncedAvailability } from '../hooks/useDebouncedAvailability'
import { useTenantRoleOptions } from '../hooks/useTenantRoleOptions'
import {
  checkTenantKeyAvailability,
  createTenant,
  inviteMember,
  listTenants,
  switchTenant,
  updateTenant,
} from '../services'
import type { Tenant, UserPickerItem } from '../types'
import { getApiErrorMessage } from '../utils/apiError'
import { tenantRoleLabel } from '../utils/enumLabels'
import { copyInviteLink } from '../utils/inviteUrl'
import { isValidTenantKey, normalizeTenantKeyInput } from '../utils/tenantKeyValidation'

const tenantKeyAvailabilityMessages = {
  checking: 'Verificando disponibilidade…',
  available: 'Chave disponível',
  unavailable: 'Chave já está em uso',
  invalid: 'Formato inválido (minúsculas, números e hífens; 2–63 caracteres)',
}

export function TenantsPage() {
  const { applyTenantSwitch, platformRoles, tenantRoles } = useAuth()
  const { tenantId: selectedTenantId, selectTenant } = useTenant()
  const isPlatformAdministrator = platformRoles.includes('plat_admin')
  const hasTenantAdministrativeRole = tenantRoles.includes('owner') || tenantRoles.includes('admin')
  const canManageTenant = isPlatformAdministrator || hasTenantAdministrativeRole

  const [tenants, setTenants] = useState<Tenant[]>([])
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [lastInviteAcceptPath, setLastInviteAcceptPath] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const [createOpen, setCreateOpen] = useState(false)
  const [editOpen, setEditOpen] = useState(false)
  const [inviteOpen, setInviteOpen] = useState(false)
  const [lookupOpen, setLookupOpen] = useState(false)

  const [newTenantName, setNewTenantName] = useState('')
  const [newTenantKey, setNewTenantKey] = useState('')
  const [createAdminUser, setCreateAdminUser] = useState<UserPickerItem | null>(null)
  const [createAdminEmail, setCreateAdminEmail] = useState('')

  const [editingTenant, setEditingTenant] = useState<Tenant | null>(null)
  const [editingTenantName, setEditingTenantName] = useState('')

  const [inviteTenant, setInviteTenant] = useState<Tenant | null>(null)
  const [inviteUser, setInviteUser] = useState<UserPickerItem | null>(null)
  const [inviteEmail, setInviteEmail] = useState('')
  const [inviteRole, setInviteRole] = useState('member')

  const [lookupTenant, setLookupTenant] = useState<Tenant | null>(null)

  const inviteTenantId = inviteTenant?.id ?? ''
  const { roleKeys: inviteRoleKeys, loading: inviteRolesLoading } = useTenantRoleOptions(inviteTenantId || null)

  const checkKeyAvailable = useCallback(
    (key: string) => checkTenantKeyAvailability(normalizeTenantKeyInput(key)),
    [],
  )
  const keyAvailability = useDebouncedAvailability(newTenantKey, checkKeyAvailable, isValidTenantKey)

  useEffect(() => {
    void loadTenants()
  }, [])

  useEffect(() => {
    if (inviteRoleKeys.length > 0 && !inviteRoleKeys.includes(inviteRole)) {
      setInviteRole(inviteRoleKeys.includes('member') ? 'member' : inviteRoleKeys[0])
    }
  }, [inviteRoleKeys, inviteRole])

  async function loadTenants(): Promise<void> {
    setError(null)
    try {
      const data = await listTenants({ page: 1, pageSize: 100 })
      setTenants(data.items)
      if (data.items.length > 0 && !inviteTenant) {
        setInviteTenant(data.items[0])
      }
    } catch (loadError) {
      setError(getApiErrorMessage(loadError))
    }
  }

  const fetchTenantsPage = useCallback(
    (query: string, page: number) => listTenants({ search: query, page, pageSize: 20 }),
    [],
  )

  function openEditDialog(tenant: Tenant): void {
    setEditingTenant(tenant)
    setEditingTenantName(tenant.name)
    setEditOpen(true)
  }

  function resetCreateForm(): void {
    setNewTenantName('')
    setNewTenantKey('')
    setCreateAdminUser(null)
    setCreateAdminEmail('')
  }

  async function handleCreate(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    if (keyAvailability === 'unavailable' || keyAvailability === 'invalid') {
      return
    }

    setLoading(true)
    setError(null)
    setSuccess(null)
    try {
      const created = await createTenant({
        name: newTenantName,
        key: normalizeTenantKeyInput(newTenantKey),
        initialAdministratorUserId: createAdminUser?.id ?? null,
        initialAdministratorEmail:
          !createAdminUser && createAdminEmail.trim() ? createAdminEmail.trim() : null,
      })
      setSuccess(
        createAdminUser || !createAdminEmail.trim()
          ? `Tenant criado: ${created.id}`
          : `Tenant criado e convite enviado para ${createAdminEmail.trim()}`,
      )
      setCreateOpen(false)
      resetCreateForm()
      await loadTenants()
    } catch (submitError) {
      setError(getApiErrorMessage(submitError))
    } finally {
      setLoading(false)
    }
  }

  async function handleUpdate(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    if (!editingTenant) {
      return
    }
    setLoading(true)
    setError(null)
    setSuccess(null)
    try {
      await updateTenant(editingTenant.id, { name: editingTenantName })
      setSuccess('Tenant atualizado com sucesso.')
      setEditOpen(false)
      await loadTenants()
    } catch (submitError) {
      setError(getApiErrorMessage(submitError))
    } finally {
      setLoading(false)
    }
  }

  async function handleInvite(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    if (!inviteTenant) {
      return
    }
    const email = inviteUser?.email ?? inviteEmail.trim()
    if (!email) {
      setError('Informe um usuário ou e-mail para o convite.')
      return
    }

    setLoading(true)
    setError(null)
    setSuccess(null)
    try {
      const result = await inviteMember(inviteTenant.id, { email, roles: [inviteRole] })
      setLastInviteAcceptPath(result.acceptPath)
      setSuccess(`Convite enviado para ${email}.`)
      setInviteOpen(false)
      setInviteUser(null)
      setInviteEmail('')
    } catch (submitError) {
      setError(getApiErrorMessage(submitError))
    } finally {
      setLoading(false)
    }
  }

  async function handleLookupSelect(): Promise<void> {
    if (!lookupTenant) {
      return
    }
    await handleSelectTenant(lookupTenant.id)
    setLookupOpen(false)
  }

  async function handleSelectTenant(tenantId: string): Promise<void> {
    setError(null)
    setSuccess(null)
    try {
      const context = await switchTenant(tenantId)
      applyTenantSwitch(context)
      selectTenant(tenantId)
      setSuccess('Tenant selecionado e sessão atualizada.')
    } catch (selectError) {
      setError(getApiErrorMessage(selectError))
    }
  }

  const createDisabled = useMemo(
    () =>
      keyAvailability === 'unavailable' ||
      keyAvailability === 'invalid' ||
      keyAvailability === 'checking',
    [keyAvailability],
  )

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Tenants"
        description="Organizações isoladas na plataforma. Selecione um tenant para operações contextuais."
        actions={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            {canManageTenant ? (
              <Button startIcon={<SearchIcon />} onClick={() => setLookupOpen(true)}>
                Buscar tenant
              </Button>
            ) : null}
            {canManageTenant ? (
              <Button startIcon={<MailOutlineIcon />} onClick={() => setInviteOpen(true)}>
                Convidar membro
              </Button>
            ) : null}
            {isPlatformAdministrator ? (
              <Button startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
                Novo tenant
              </Button>
            ) : null}
          </Stack>
        }
      />
      <FeedbackAlerts success={success} error={error} />
      {lastInviteAcceptPath ? (
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <Button
            size="small"
            startIcon={<ContentCopyIcon />}
            onClick={() => {
              void copyInviteLink(lastInviteAcceptPath).then(() => {
                setSuccess('Link do convite copiado.')
              })
            }}
          >
            Copiar link do convite
          </Button>
        </Stack>
      ) : null}

      <SectionCard title="Tenants cadastrados">
        <DataTable
          columns={[
            { id: 'id', label: 'Id', minWidth: 120 },
            { id: 'name', label: 'Nome' },
            { id: 'key', label: 'Chave' },
            { id: 'actions', label: 'Ações', align: 'right' },
          ]}
          rows={tenants.map((tenant) => (
            <TableRow key={tenant.id} hover>
              <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>{tenant.id}</TableCell>
              <TableCell>{tenant.name}</TableCell>
              <TableCell>{tenant.key}</TableCell>
              <TableCell align="right">
                <Stack direction="row" spacing={0.5} sx={{ justifyContent: 'flex-end', alignItems: 'center' }}>
                  {canManageTenant ? (
                    <Tooltip title="Editar tenant">
                      <IconButton size="small" onClick={() => openEditDialog(tenant)}>
                        <EditOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  ) : null}
                  {selectedTenantId === tenant.id ? (
                    <StatusChip label="Selecionado" variant="success" />
                  ) : (
                    <Button size="small" onClick={() => void handleSelectTenant(tenant.id)}>
                      Selecionar
                    </Button>
                  )}
                </Stack>
              </TableCell>
            </TableRow>
          ))}
          emptyDescription="Nenhum tenant cadastrado."
        />
      </SectionCard>

      <ResourceDialog
        open={createOpen}
        onClose={() => {
          setCreateOpen(false)
          resetCreateForm()
        }}
        title="Novo tenant"
        description="Crie uma nova organização na plataforma."
        loading={loading}
        submitLabel="Criar"
        onSubmit={handleCreate}
        disableSubmit={createDisabled}
      >
        <FormSection title="Organização" description="Nome e chave única do tenant.">
          <FormGrid>
            <FormGridItem>
              <TextField label="Nome" value={newTenantName} onChange={(e) => setNewTenantName(e.target.value)} required fullWidth />
            </FormGridItem>
            <FormGridItem>
              <AvailabilityTextField
                label="Chave"
                value={newTenantKey}
                onChange={(e) => setNewTenantKey(e.target.value)}
                required
                fullWidth
                availabilityStatus={keyAvailability}
                availabilityMessages={tenantKeyAvailabilityMessages}
                idleHelperText="Letras minúsculas, números e hífens"
              />
            </FormGridItem>
          </FormGrid>
        </FormSection>
        <FormSection title="Administrador inicial" dividerBefore description="Opcional: usuário existente ou convite por e-mail.">
          <UserPickerField
            label="Administrador inicial"
            selectedUser={createAdminUser}
            onUserChange={setCreateAdminUser}
            inviteEmail={createAdminEmail}
            onInviteEmailChange={setCreateAdminEmail}
            disabled={loading}
          />
        </FormSection>
      </ResourceDialog>

      <ResourceDialog
        open={editOpen}
        onClose={() => setEditOpen(false)}
        title="Editar tenant"
        description={editingTenant ? `Chave: ${editingTenant.key}` : undefined}
        loading={loading}
        submitLabel="Salvar"
        onSubmit={handleUpdate}
      >
        <FormSection title="Identificação">
          <FormGrid>
            <FormGridItem xs={12} md={12}>
              <StaticField label="Tenant Id" value={editingTenant?.id} monospace />
            </FormGridItem>
            <FormGridItem>
              <TextField
                label="Nome"
                value={editingTenantName}
                onChange={(e) => setEditingTenantName(e.target.value)}
                required
                fullWidth
              />
            </FormGridItem>
          </FormGrid>
        </FormSection>
      </ResourceDialog>

      <ResourceDialog
        open={inviteOpen}
        onClose={() => setInviteOpen(false)}
        title="Convidar membro"
        description="Busque um usuário ou convide por e-mail. Aceite em /accept-invite."
        loading={loading}
        submitLabel="Enviar convite"
        onSubmit={handleInvite}
        disableSubmit={!inviteTenant}
      >
        <FormSection title="Convite" description="Tenant, usuário e papel do novo membro.">
          <FormGrid>
            <FormGridItem xs={12} md={12}>
              <PaginatedAutocomplete
                label="Tenant"
                value={inviteTenant}
                onChange={setInviteTenant}
                fetchPage={fetchTenantsPage}
                getOptionLabel={(t) => `${t.name} (${t.key})`}
                isOptionEqualToValue={(a, b) => a.id === b.id}
              />
            </FormGridItem>
            <FormGridItem xs={12} md={12}>
              <UserPickerField
                selectedUser={inviteUser}
                onUserChange={setInviteUser}
                inviteEmail={inviteEmail}
                onInviteEmailChange={setInviteEmail}
                disabled={loading}
              />
            </FormGridItem>
            <FormGridItem>
              <TextField
                select
                label="Papel"
                value={inviteRole}
                onChange={(e) => setInviteRole(e.target.value)}
                fullWidth
                disabled={inviteRolesLoading || inviteRoleKeys.length === 0}
              >
                {inviteRoleKeys.map((role) => (
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
        open={lookupOpen}
        onClose={() => {
          setLookupOpen(false)
          setLookupTenant(null)
        }}
        title="Buscar tenant"
        description="Pesquise por nome ou chave (mínimo 3 caracteres)."
        loading={loading}
        submitLabel="Selecionar tenant"
        onSubmit={(event) => {
          event.preventDefault()
          void handleLookupSelect()
        }}
        disableSubmit={!lookupTenant}
      >
        <FormSection title="Consulta">
          <FormGrid>
            <FormGridItem xs={12} md={12}>
              <PaginatedAutocomplete
                label="Tenant"
                value={lookupTenant}
                onChange={setLookupTenant}
                fetchPage={fetchTenantsPage}
                getOptionLabel={(t) => `${t.name} (${t.key})`}
                isOptionEqualToValue={(a, b) => a.id === b.id}
              />
            </FormGridItem>
            {lookupTenant ? (
              <FormGridItem xs={12} md={12}>
                <Typography variant="body2" color="text.secondary">
                  Chave: <strong>{lookupTenant.key}</strong> · ID: {lookupTenant.id}
                </Typography>
              </FormGridItem>
            ) : null}
          </FormGrid>
        </FormSection>
      </ResourceDialog>
    </Stack>
  )
}
