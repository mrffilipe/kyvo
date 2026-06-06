import AddIcon from '@mui/icons-material/Add'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  FormControlLabel,
  FormGroup,
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
  ConfirmDialog,
  DataTable,
  FeedbackAlerts,
  FormGrid,
  FormGridItem,
  FormSection,
  PageHeader,
  ResourceDialog,
  SectionCard,
  SteppedFormDialog,
} from '../components/ui'
import { useAuth } from '../contexts/AuthContext'
import { useDebouncedAvailability } from '../hooks/useDebouncedAvailability'
import {
  addIdentityProvider,
  checkIdentityProviderAliasAvailability,
  disableIdentityProvider,
  enableIdentityProvider,
  listIdentityProviders,
  updateIdentityProvider,
} from '../services'
import {
  emptyFirebaseConfigFields,
  FirebaseProviderConfigForm,
} from '../components/identityProviders/FirebaseProviderConfigForm'
import {
  IdentityProviderType,
  IdpCapability,
  type AddIdentityProviderBody,
  type IdentityProviderDto,
  type UpdateIdentityProviderBody,
} from '../types'
import { getApiErrorMessage } from '../utils/apiError'
import {
  buildFirebaseConfigJson,
  isBootstrapLocalProvider,
  validateFirebaseConfigFields,
  type FirebaseConfigFieldValues,
} from '../utils/firebaseIdpConfig'
import { isValidIdpAlias } from '../utils/idpAliasValidation'

const ADD_STEPS = ['Identificação', 'Capacidades', 'Configuração'] as const

const aliasAvailabilityMessages = {
  checking: 'Verificando disponibilidade…',
  available: 'Alias disponível',
  unavailable: 'Alias já está em uso',
  invalid: 'Use letras minúsculas, dígitos, hífens ou sublinhados',
}

const providerTypeLabels: Record<IdentityProviderType, string> = {
  [IdentityProviderType.Local]: 'Local (e-mail/senha)',
  [IdentityProviderType.Firebase]: 'Firebase',
  [IdentityProviderType.Cognito]: 'Amazon Cognito',
  [IdentityProviderType.Generic]: 'OIDC genérico',
}

/** Tipos disponíveis ao cadastrar (o Local é criado no bootstrap). */
const providerTypeOptions: Array<{ label: string; value: IdentityProviderType }> = [
  { label: providerTypeLabels[IdentityProviderType.Firebase], value: IdentityProviderType.Firebase },
]

const capabilityOptions: Array<{ value: IdpCapability; label: string; locked?: IdentityProviderType }> = [
  { value: IdpCapability.LocalPassword, label: 'E-mail e senha (local)', locked: IdentityProviderType.Local },
  { value: IdpCapability.GoogleSocial, label: 'Entrar com Google' },
  { value: IdpCapability.MicrosoftSocial, label: 'Entrar com Microsoft' },
  { value: IdpCapability.AppleSocial, label: 'Entrar com Apple' },
  { value: IdpCapability.GenericOidc, label: 'OIDC genérico' },
]

function defaultCapabilitiesFor(type: IdentityProviderType): IdpCapability[] {
  switch (type) {
    case IdentityProviderType.Firebase:
      return [IdpCapability.GoogleSocial]
    case IdentityProviderType.Cognito:
    case IdentityProviderType.Generic:
      return [IdpCapability.GenericOidc]
    default:
      return []
  }
}

function isCapabilityAllowed(type: IdentityProviderType, capability: IdpCapability): boolean {
  if (capability === IdpCapability.LocalPassword) {
    return type === IdentityProviderType.Local
  }
  return type !== IdentityProviderType.Local
}

function providerTypeLabel(type: IdentityProviderType | undefined): string {
  if (type === undefined) {
    return '—'
  }
  return providerTypeLabels[type] ?? String(type)
}

function capabilityLabel(capability: IdpCapability): string {
  return capabilityOptions.find((o) => o.value === capability)?.label ?? capability
}

const CONFIG_SCHEMA_HINTS: Partial<Record<IdentityProviderType, string>> = {
  [IdentityProviderType.Cognito]:
    'Obrigatório: userPoolId, region, clientId. Login Cognito ainda não implementado — apenas cadastro.',
  [IdentityProviderType.Generic]:
    'Obrigatório: issuer, jwksUri, audience. Login OIDC genérico ainda não implementado — apenas cadastro.',
}

function validateLegacyConfigJson(json: string): string | null {
  const trimmed = json.trim()
  if (!trimmed) {
    return 'A configuração JSON é obrigatória para este tipo de provedor.'
  }
  try {
    JSON.parse(trimmed)
    return null
  } catch {
    return 'Configuração JSON inválida: verifique a sintaxe.'
  }
}

export function IdentityProvidersPage() {
  const { platformRoles } = useAuth()
  const isPlatformAdministrator = platformRoles.includes('plat_admin')

  const [items, setItems] = useState<IdentityProviderDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [warnings, setWarnings] = useState<string[]>([])
  const [loading, setLoading] = useState(false)

  const [addOpen, setAddOpen] = useState(false)
  const [addStep, setAddStep] = useState(0)
  const [alias, setAlias] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [providerType] = useState<IdentityProviderType>(IdentityProviderType.Firebase)
  const [capabilities, setCapabilities] = useState<IdpCapability[]>(defaultCapabilitiesFor(IdentityProviderType.Firebase))
  const [firebaseFields, setFirebaseFields] = useState<FirebaseConfigFieldValues>(emptyFirebaseConfigFields)
  const [firebaseFileError, setFirebaseFileError] = useState<string | null>(null)

  const [editOpen, setEditOpen] = useState(false)
  const [editId, setEditId] = useState('')
  const [editDisplayName, setEditDisplayName] = useState('')
  const [editConfigJson, setEditConfigJson] = useState('')
  const [editCapabilities, setEditCapabilities] = useState<IdpCapability[]>([])
  const [editProviderType, setEditProviderType] = useState<IdentityProviderType>(IdentityProviderType.Firebase)
  const [editFirebaseFields, setEditFirebaseFields] = useState<FirebaseConfigFieldValues>(emptyFirebaseConfigFields)
  const [editFirebaseFileError, setEditFirebaseFileError] = useState<string | null>(null)
  const [deactivateTarget, setDeactivateTarget] = useState<IdentityProviderDto | null>(null)
  const [toggleLoading, setToggleLoading] = useState(false)

  const hasBootstrapLocal = useMemo(
    () => items.some((item) => isBootstrapLocalProvider(item.providerType, item.alias)),
    [items],
  )

  const checkAliasAvailable = useCallback(
    (value: string) => checkIdentityProviderAliasAvailability(value),
    [],
  )
  const aliasAvailability = useDebouncedAvailability(alias, checkAliasAvailable, isValidIdpAlias)

  useEffect(() => {
    void loadProviders()
  }, [])

  useEffect(() => {
    if (!addOpen) {
      return
    }
    setCapabilities(defaultCapabilitiesFor(providerType))
    setFirebaseFields(emptyFirebaseConfigFields())
    setFirebaseFileError(null)
  }, [providerType, addOpen])

  async function loadProviders(): Promise<void> {
    setError(null)
    try {
      const result = await listIdentityProviders()
      setItems(result)
    } catch (loadError) {
      setError(getApiErrorMessage(loadError))
    }
  }

  function openAddDialog(): void {
    setAlias('')
    setDisplayName('')
    setCapabilities(defaultCapabilitiesFor(IdentityProviderType.Firebase))
    setFirebaseFields(emptyFirebaseConfigFields())
    setFirebaseFileError(null)
    setAddStep(0)
    setAddOpen(true)
  }

  function openEditDialog(item: IdentityProviderDto): void {
    if (isBootstrapLocalProvider(item.providerType, item.alias)) {
      return
    }
    setEditId(item.id)
    setEditDisplayName(item.displayName)
    setEditProviderType(item.providerType)
    setEditCapabilities(item.capabilities ?? [])
    setEditConfigJson('')
    setEditFirebaseFields(emptyFirebaseConfigFields())
    setEditFirebaseFileError(null)
    setEditOpen(true)
  }

  function toggleCapability(
    list: IdpCapability[],
    setList: (next: IdpCapability[]) => void,
    capability: IdpCapability,
    checked: boolean,
  ): void {
    if (checked && !list.includes(capability)) {
      setList([...list, capability])
    } else if (!checked) {
      setList(list.filter((c) => c !== capability))
    }
  }

  function isAddStepValid(): boolean {
    switch (addStep) {
      case 0:
        return (
          alias.trim().length > 0
          && displayName.trim().length > 0
          && aliasAvailability !== 'unavailable'
          && aliasAvailability !== 'invalid'
          && aliasAvailability !== 'checking'
        )
      case 1:
        return capabilities.length > 0
      case 2:
        return validateFirebaseConfigFields(firebaseFields, 'create') === null && !firebaseFileError
      default:
        return false
    }
  }

  function resolveEditConfigJson(): { configJson?: string | null; error: string | null } {
    if (editProviderType === IdentityProviderType.Firebase) {
      const validationError = validateFirebaseConfigFields(editFirebaseFields, 'update')
      if (validationError) {
        return { error: validationError }
      }
      const hasNewConfig =
        editFirebaseFields.projectId.trim() ||
        editFirebaseFields.webApiKey.trim() ||
        editFirebaseFields.serviceAccount
      if (!hasNewConfig) {
        return { configJson: undefined, error: null }
      }
      return { configJson: buildFirebaseConfigJson(editFirebaseFields), error: null }
    }

    if (!editConfigJson.trim()) {
      return { configJson: undefined, error: null }
    }
    const legacyError = validateLegacyConfigJson(editConfigJson)
    if (legacyError) {
      return { error: legacyError }
    }
    return { configJson: editConfigJson.trim(), error: null }
  }

  async function handleAdd(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    const firebaseError = validateFirebaseConfigFields(firebaseFields, 'create')
    if (firebaseError) {
      setError(firebaseError)
      return
    }
    if (capabilities.length === 0) {
      setError('Selecione ao menos uma capacidade oferecida por este provedor.')
      return
    }
    setLoading(true)
    setError(null)
    setSuccess(null)
    setWarnings([])
    const body: AddIdentityProviderBody = {
      alias,
      displayName,
      providerType,
      capabilities,
      configJson: buildFirebaseConfigJson(firebaseFields),
    }
    try {
      const result = await addIdentityProvider(body)
      setSuccess('Provedor de identidade adicionado.')
      setWarnings(result.warnings ?? [])
      setAddOpen(false)
      await loadProviders()
    } catch (addError) {
      setError(getApiErrorMessage(addError))
    } finally {
      setLoading(false)
    }
  }

  async function handleEdit(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    const { configJson, error: configError } = resolveEditConfigJson()
    if (configError) {
      setError(configError)
      return
    }
    setLoading(true)
    setError(null)
    setSuccess(null)
    const body: UpdateIdentityProviderBody = {
      displayName: editDisplayName,
      capabilities: editCapabilities,
      ...(configJson !== undefined ? { configJson } : {}),
    }
    try {
      await updateIdentityProvider(editId, body)
      setSuccess('Provedor de identidade atualizado.')
      setEditOpen(false)
      await loadProviders()
    } catch (editError) {
      setError(getApiErrorMessage(editError))
    } finally {
      setLoading(false)
    }
  }

  async function handleToggle(item: IdentityProviderDto): Promise<void> {
    setError(null)
    setSuccess(null)
    setToggleLoading(true)
    try {
      if (item.enabled) {
        await disableIdentityProvider(item.id)
        setSuccess(`"${item.displayName}" desativado.`)
      } else {
        await enableIdentityProvider(item.id)
        setSuccess(`"${item.displayName}" ativado.`)
      }
      setDeactivateTarget(null)
      await loadProviders()
    } catch (toggleError) {
      setError(getApiErrorMessage(toggleError))
    } finally {
      setToggleLoading(false)
    }
  }

  function handleToggleClick(item: IdentityProviderDto): void {
    if (item.enabled) {
      setDeactivateTarget(item)
      return
    }
    void handleToggle(item)
  }

  if (!isPlatformAdministrator) {
    return (
      <Stack spacing={3}>
        <PageHeader
          title="Provedores de identidade"
          description="Gerencie os provedores de identidade da plataforma."
        />
        <Alert severity="warning">
          Somente administradores da plataforma podem gerenciar provedores de identidade.
        </Alert>
      </Stack>
    )
  }

  function renderCapabilityCheckboxes(
    selectedType: IdentityProviderType,
    selected: IdpCapability[],
    onChange: (next: IdpCapability[]) => void,
  ) {
    return (
      <FormGroup>
        {capabilityOptions.map((option) => {
          const allowed = isCapabilityAllowed(selectedType, option.value)
          const isLocal = option.value === IdpCapability.LocalPassword
          const lockedOn = isLocal && selectedType === IdentityProviderType.Local
          return (
            <FormControlLabel
              key={option.value}
              control={
                <Checkbox
                  checked={selected.includes(option.value) || lockedOn}
                  disabled={!allowed || lockedOn}
                  onChange={(event) => toggleCapability(selected, onChange, option.value, event.target.checked)}
                />
              }
              label={option.label}
            />
          )
        })}
      </FormGroup>
    )
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Provedores de identidade"
        description="Gerencie os provedores de identidade habilitados na plataforma (Local do bootstrap, Firebase, etc.)."
        actions={
          <Button startIcon={<AddIcon />} onClick={openAddDialog}>
            Adicionar IdP
          </Button>
        }
      />

      <FeedbackAlerts success={success} error={error} />

      {hasBootstrapLocal ? (
        <Alert severity="info">
          O provedor <strong>Local (e-mail/senha)</strong> é criado automaticamente no bootstrap da plataforma e não
          pode ser cadastrado, editado ou desativado por aqui.
        </Alert>
      ) : null}

      {warnings.length > 0 && (
        <Alert severity="warning" onClose={() => setWarnings([])}>
          <Typography variant="subtitle2" component="div" sx={{ mb: 1 }}>
            Conflitos de capacidade detectados
          </Typography>
          <Stack spacing={0.5} component="ul" sx={{ pl: 2, m: 0 }}>
            {warnings.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </Stack>
        </Alert>
      )}

      <SectionCard title="Provedores cadastrados">
        <DataTable
          columns={[
            { id: 'alias', label: 'Alias' },
            { id: 'displayName', label: 'Nome' },
            { id: 'type', label: 'Tipo' },
            { id: 'capabilities', label: 'Capacidades' },
            { id: 'status', label: 'Status' },
            { id: 'actions', label: 'Ações', align: 'right' },
          ]}
          rows={items.map((item) => {
            const isLocalBootstrap = isBootstrapLocalProvider(item.providerType, item.alias)
            return (
              <TableRow key={item.id} hover>
                <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.8rem' }}>{item.alias}</TableCell>
                <TableCell>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                    <span>{item.displayName}</span>
                    {isLocalBootstrap ? (
                      <Chip label="Bootstrap" size="small" variant="outlined" color="info" />
                    ) : null}
                  </Stack>
                </TableCell>
                <TableCell>{providerTypeLabel(item.providerType)}</TableCell>
                <TableCell>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {(item.capabilities ?? []).map((capability) => (
                      <Chip key={capability} label={capabilityLabel(capability)} size="small" variant="outlined" />
                    ))}
                  </Box>
                </TableCell>
                <TableCell>
                  <Chip
                    label={item.enabled ? 'Ativo' : 'Inativo'}
                    size="small"
                    color={item.enabled ? 'success' : 'default'}
                    variant="outlined"
                  />
                </TableCell>
                <TableCell align="right">
                  <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                    {isLocalBootstrap ? (
                      <Tooltip title="O provedor local do bootstrap não pode ser editado">
                        <span>
                          <Button size="small" disabled>
                            Editar
                          </Button>
                        </span>
                      </Tooltip>
                    ) : (
                      <Button size="small" onClick={() => openEditDialog(item)}>
                        Editar
                      </Button>
                    )}
                    {isLocalBootstrap ? (
                      <Tooltip title="O provedor local do bootstrap não pode ser desativado">
                        <span>
                          <Button size="small" disabled>
                            Desativar
                          </Button>
                        </span>
                      </Tooltip>
                    ) : (
                      <Button
                        size="small"
                        color={item.enabled ? 'warning' : 'success'}
                        onClick={() => handleToggleClick(item)}
                      >
                        {item.enabled ? 'Desativar' : 'Ativar'}
                      </Button>
                    )}
                  </Stack>
                </TableCell>
              </TableRow>
            )
          })}
          emptyDescription="Nenhum provedor cadastrado ainda. Adicione um provedor Firebase para federação."
        />
      </SectionCard>

      <SteppedFormDialog
        open={addOpen}
        onClose={() => setAddOpen(false)}
        title="Adicionar provedor de identidade"
        description="Cadastre um provedor Firebase para login social na plataforma."
        steps={ADD_STEPS}
        activeStep={addStep}
        loading={loading}
        submitLabel="Adicionar"
        onBack={() => setAddStep((step) => step - 1)}
        onNext={() => setAddStep((step) => step + 1)}
        onSubmit={handleAdd}
        disableNext={!isAddStepValid()}
        disableSubmit={!isAddStepValid()}
      >
        {addStep === 0 ? (
          <FormSection title="Identificação" description="Alias único, nome exibido e tipo do provedor.">
            <FormGrid>
              <FormGridItem>
                <AvailabilityTextField
                  label="Alias"
                  value={alias}
                  onChange={(event) => setAlias(event.target.value)}
                  required
                  fullWidth
                  availabilityStatus={aliasAvailability}
                  availabilityMessages={aliasAvailabilityMessages}
                  idleHelperText="Identificador único (letras minúsculas, dígitos, hífens ou sublinhados)."
                />
              </FormGridItem>
              <FormGridItem>
                <TextField
                  label="Nome de exibição"
                  value={displayName}
                  onChange={(event) => setDisplayName(event.target.value)}
                  required
                  fullWidth
                />
              </FormGridItem>
              <FormGridItem xs={12}>
                <TextField select label="Tipo" value={providerType} fullWidth disabled>
                  {providerTypeOptions.map((option) => (
                    <MenuItem key={option.value} value={option.value}>
                      {option.label}
                    </MenuItem>
                  ))}
                </TextField>
              </FormGridItem>
            </FormGrid>
          </FormSection>
        ) : null}

        {addStep === 1 ? (
          <FormSection title="Capacidades" description="Métodos de autenticação oferecidos na tela de login.">
            <Typography variant="caption" component="div" color="text.secondary" sx={{ mb: 1 }}>
              E-mail e senha permanece no provedor Local do bootstrap. Capacidades sociais permitem vários
              provedores Firebase, mas podem gerar avisos em caso de conflito.
            </Typography>
            {renderCapabilityCheckboxes(providerType, capabilities, setCapabilities)}
          </FormSection>
        ) : null}

        {addStep === 2 ? (
          <FormSection title="Configuração">
            <FirebaseProviderConfigForm
              mode="create"
              values={firebaseFields}
              onChange={setFirebaseFields}
              fileError={firebaseFileError}
              onFileError={setFirebaseFileError}
            />
          </FormSection>
        ) : null}
      </SteppedFormDialog>

      <ResourceDialog
        open={editOpen}
        onClose={() => setEditOpen(false)}
        title="Editar provedor de identidade"
        description="Atualize o nome de exibição, as capacidades ou a configuração."
        loading={loading}
        submitLabel="Salvar"
        onSubmit={handleEdit}
      >
        <FormSection title="Identificação">
          <FormGrid>
            <FormGridItem xs={12} md={12}>
              <TextField
                label="Nome de exibição"
                value={editDisplayName}
                onChange={(event) => setEditDisplayName(event.target.value)}
                required
                fullWidth
              />
            </FormGridItem>
            <FormGridItem xs={12}>
              <Typography variant="body2" color="text.secondary">
                Tipo: <strong>{providerTypeLabel(editProviderType)}</strong>
              </Typography>
            </FormGridItem>
          </FormGrid>
        </FormSection>

        <FormSection title="Capacidades" dividerBefore>
          {renderCapabilityCheckboxes(editProviderType, editCapabilities, setEditCapabilities)}
        </FormSection>

        {editProviderType === IdentityProviderType.Firebase ? (
          <FormSection title="Configuração" dividerBefore>
            <FirebaseProviderConfigForm
              mode="update"
              values={editFirebaseFields}
              onChange={setEditFirebaseFields}
              fileError={editFirebaseFileError}
              onFileError={setEditFirebaseFileError}
            />
          </FormSection>
        ) : (
          <FormSection title="Configuração" dividerBefore>
            <FormGrid>
              {CONFIG_SCHEMA_HINTS[editProviderType] ? (
                <FormGridItem xs={12}>
                  <Alert severity="info" sx={{ width: '100%' }}>
                    {CONFIG_SCHEMA_HINTS[editProviderType]}
                  </Alert>
                </FormGridItem>
              ) : null}
              {(editProviderType === IdentityProviderType.Cognito ||
                editProviderType === IdentityProviderType.Generic) && (
                <FormGridItem xs={12}>
                  <Alert severity="warning" sx={{ width: '100%' }}>
                    Login ainda não implementado para este tipo; o cadastro prepara o provedor para uso futuro.
                  </Alert>
                </FormGridItem>
              )}
              <FormGridItem xs={12}>
                <TextField
                  label="Configuração (JSON)"
                  value={editConfigJson}
                  onChange={(event) => setEditConfigJson(event.target.value)}
                  fullWidth
                  multiline
                  minRows={6}
                  helperText="Deixe vazio para manter a configuração atual."
                  slotProps={{ input: { sx: { fontFamily: 'monospace', fontSize: '0.75rem', lineHeight: 1.4 } } }}
                />
              </FormGridItem>
            </FormGrid>
          </FormSection>
        )}
      </ResourceDialog>

      <ConfirmDialog
        open={deactivateTarget !== null}
        onClose={() => setDeactivateTarget(null)}
        onConfirm={() => {
          if (deactivateTarget) {
            void handleToggle(deactivateTarget)
          }
        }}
        title="Desativar provedor de identidade"
        message={
          deactivateTarget
            ? `Deseja desativar "${deactivateTarget.displayName}"? Usuários não poderão autenticar por este provedor até que seja reativado.`
            : ''
        }
        confirmLabel="Desativar"
        confirmColor="warning"
        loading={toggleLoading}
      />
    </Stack>
  )
}
