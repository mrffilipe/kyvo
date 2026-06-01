import AddIcon from '@mui/icons-material/Add'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import {
  Alert,
  Box,
  Button,
  Chip,
  FormGroup,
  IconButton,
  MenuItem,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router'
import {
  ApplicationBrandingFields,
  validateBrandingFields,
  type ApplicationBrandingFieldsValue,
} from '../components/applications/ApplicationBrandingFields'
import {
  AvailabilityTextField,
  CheckboxField,
  FeedbackAlerts,
  FormGrid,
  FormGridItem,
  FormSection,
  PageHeader,
  SectionCard,
  SteppedFormDialog,
  UserPickerField,
} from '../components/ui'
import { useAuth } from '../contexts/AuthContext'
import { OAUTH_SCOPE_OPTIONS } from '../constants/oauthScopes'
import { useDebouncedAvailability } from '../hooks/useDebouncedAvailability'
import {
  checkTenantKeyAvailability,
  createApplicationClient,
  getApplicationById,
  persistApplicationBranding,
  provisionApplicationTenant,
} from '../services'
import { ClientType, type Application, type UserPickerItem } from '../types'
import { defaultBrandingPrimary, defaultBrandingSecondary } from '../utils/brandingUtils'
import { getApiErrorMessage } from '../utils/apiError'
import { applicationTypeLabel } from '../utils/enumLabels'
import { isValidRedirectUri } from '../utils/urlValidation'
import { isValidTenantKey, normalizeTenantKeyInput } from '../utils/tenantKeyValidation'

const clientTypeOptions: Array<{ label: string; value: ClientType }> = [
  { label: 'Público', value: ClientType.Public },
  { label: 'Confidencial', value: ClientType.Confidential },
]

const clientSteps = ['Credenciais', 'URIs e tokens'] as const
const provisionSteps = ['Tenant', 'Metadados'] as const

function parseRedirectUris(raw: string): string[] {
  return raw
    .split(/[\n,;]+/)
    .map((value) => value.trim())
    .filter(Boolean)
}

const tenantKeyAvailabilityMessages = {
  checking: 'Verificando disponibilidade…',
  available: 'Chave disponível',
  unavailable: 'Chave já está em uso',
  invalid: 'Formato inválido (minúsculas, números e hífens)',
}

export function ApplicationDetailPage() {
  const { applicationId } = useParams()
  const { platformRoles } = useAuth()
  const [application, setApplication] = useState<Application | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const [clientOpen, setClientOpen] = useState(false)
  const [clientStep, setClientStep] = useState(0)
  const [provisionOpen, setProvisionOpen] = useState(false)
  const [provisionStep, setProvisionStep] = useState(0)

  const [clientId, setClientId] = useState('')
  const [clientSecretHash, setClientSecretHash] = useState('')
  const [clientType, setClientType] = useState<ClientType>(ClientType.Public)
  const [redirectUris, setRedirectUris] = useState('')
  const [selectedScopes, setSelectedScopes] = useState<string[]>(['openid', 'profile'])
  const [accessTokenTtlSeconds, setAccessTokenTtlSeconds] = useState('3600')
  const [redirectUriError, setRedirectUriError] = useState<string | null>(null)

  const [tenantName, setTenantName] = useState('')
  const [tenantKey, setTenantKey] = useState('')
  const [provisionAdminUser, setProvisionAdminUser] = useState<UserPickerItem | null>(null)
  const [provisionAdminEmail, setProvisionAdminEmail] = useState('')
  const [externalCustomerId, setExternalCustomerId] = useState('')
  const [planCode, setPlanCode] = useState('')
  const [brandingFields, setBrandingFields] = useState<ApplicationBrandingFieldsValue>({
    brandingEnabled: false,
    brandingPrimaryColor: defaultBrandingPrimary,
    brandingSecondaryColor: defaultBrandingSecondary,
    brandingHeroTitle: '',
    brandingHeroSubtitle: '',
    logoFile: null,
  })
  const [brandingSaving, setBrandingSaving] = useState(false)

  const isPlatformAdministrator = platformRoles.includes('plat_admin')
  const isSystemApp = application?.isSystem ?? false
  const canCreateClient = isPlatformAdministrator && !isSystemApp
  const canProvisionTenant = isPlatformAdministrator && !isSystemApp

  const checkKeyAvailable = useCallback(
    (key: string) => checkTenantKeyAvailability(normalizeTenantKeyInput(key)),
    [],
  )
  const tenantKeyAvailability = useDebouncedAvailability(tenantKey, checkKeyAvailable, isValidTenantKey)

  useEffect(() => {
    if (!applicationId) {
      return
    }
    void loadApplication(applicationId)
  }, [applicationId])

  async function loadApplication(id: string): Promise<void> {
    setError(null)
    try {
      const data = await getApplicationById(id)
      setApplication(data)
      setBrandingFields({
        brandingEnabled: data.brandingEnabled,
        brandingPrimaryColor: data.brandingPrimaryColor ?? defaultBrandingPrimary,
        brandingSecondaryColor: data.brandingSecondaryColor ?? defaultBrandingSecondary,
        brandingHeroTitle: data.brandingHeroTitle ?? '',
        brandingHeroSubtitle: data.brandingHeroSubtitle ?? '',
        logoFile: null,
      })
    } catch (loadError) {
      setError(getApiErrorMessage(loadError))
    }
  }

  function openClientDialog(): void {
    setClientStep(0)
    setClientId('')
    setClientSecretHash('')
    setClientType(ClientType.Public)
    setRedirectUris('')
    setSelectedScopes(['openid', 'profile'])
    setAccessTokenTtlSeconds('3600')
    setRedirectUriError(null)
    setClientOpen(true)
  }

  function openProvisionDialog(): void {
    setProvisionStep(0)
    setTenantName('')
    setTenantKey('')
    setProvisionAdminUser(null)
    setProvisionAdminEmail('')
    setExternalCustomerId('')
    setPlanCode('')
    setProvisionOpen(true)
  }

  function toggleScope(scope: string): void {
    setSelectedScopes((prev) =>
      prev.includes(scope) ? prev.filter((item) => item !== scope) : [...prev, scope],
    )
  }

  function validateRedirectUrisInput(): boolean {
    const uris = parseRedirectUris(redirectUris)
    if (uris.length === 0) {
      setRedirectUriError('Informe pelo menos uma redirect URI.')
      return false
    }
    const invalid = uris.find((uri) => !isValidRedirectUri(uri))
    if (invalid) {
      setRedirectUriError(`URL inválida: ${invalid}`)
      return false
    }
    setRedirectUriError(null)
    return true
  }

  async function handleCreateClient(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    if (!canCreateClient || !applicationId) {
      setError('Sem permissão para criar client.')
      return
    }
    if (!validateRedirectUrisInput()) {
      return
    }
    if (selectedScopes.length === 0) {
      setError('Selecione pelo menos um scope permitido.')
      return
    }

    setLoading(true)
    setError(null)
    setSuccess(null)
    try {
      const created = await createApplicationClient(applicationId, {
        clientId,
        clientSecretHash: clientSecretHash || null,
        clientType,
        redirectUris: parseRedirectUris(redirectUris).join('\n'),
        allowedScopesList: selectedScopes,
        accessTokenTtlSeconds: Number(accessTokenTtlSeconds),
      })
      setSuccess(`Client criado: ${created.id}`)
      setClientOpen(false)
    } catch (createError) {
      setError(getApiErrorMessage(createError))
    } finally {
      setLoading(false)
    }
  }

  async function handleProvisionTenant(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()
    if (!applicationId || !isPlatformAdministrator) {
      return
    }
    if (
      tenantKeyAvailability === 'unavailable' ||
      tenantKeyAvailability === 'invalid' ||
      tenantKeyAvailability === 'checking'
    ) {
      return
    }

    setLoading(true)
    setError(null)
    setSuccess(null)
    try {
      const provisioned = await provisionApplicationTenant(applicationId, {
        tenantName,
        tenantKey: normalizeTenantKeyInput(tenantKey),
        initialAdministratorUserId: provisionAdminUser?.id ?? null,
        initialAdministratorEmail:
          !provisionAdminUser && provisionAdminEmail.trim() ? provisionAdminEmail.trim() : null,
        externalCustomerId: externalCustomerId.trim() || null,
        planCode: planCode.trim() || null,
      })
      setSuccess(
        provisionAdminEmail.trim() && !provisionAdminUser
          ? `Tenant provisionado (${provisioned.tenantId}) e convite enviado.`
          : `Tenant provisionado: ${provisioned.tenantId}`,
      )
      setProvisionOpen(false)
    } catch (provisionError) {
      setError(getApiErrorMessage(provisionError))
    } finally {
      setLoading(false)
    }
  }

  async function handleSaveBranding(): Promise<void> {
    if (!applicationId || !isPlatformAdministrator || isSystemApp) {
      return
    }

    const brandingValidation = validateBrandingFields(brandingFields)
    if (brandingValidation) {
      setError(brandingValidation)
      return
    }

    setBrandingSaving(true)
    setError(null)
    setSuccess(null)
    try {
      await persistApplicationBranding(applicationId, brandingFields)
      await loadApplication(applicationId)
      setSuccess('Identidade visual salva.')
    } catch (saveError) {
      setError(getApiErrorMessage(saveError))
    } finally {
      setBrandingSaving(false)
    }
  }

  async function copySecret(): Promise<void> {
    if (clientSecretHash) {
      await navigator.clipboard.writeText(clientSecretHash)
    }
  }

  const clientStepValid =
    clientStep === 0
      ? Boolean(clientId.trim())
      : Boolean(accessTokenTtlSeconds.trim()) &&
        selectedScopes.length > 0 &&
        parseRedirectUris(redirectUris).length > 0 &&
        parseRedirectUris(redirectUris).every(isValidRedirectUri)

  const provisionStepValid = useMemo(
    () =>
      provisionStep === 0
        ? Boolean(tenantName.trim() && tenantKey.trim()) &&
          tenantKeyAvailability !== 'unavailable' &&
          tenantKeyAvailability !== 'invalid' &&
          tenantKeyAvailability !== 'checking'
        : true,
    [provisionStep, tenantName, tenantKey, tenantKeyAvailability],
  )

  return (
    <Stack spacing={3}>
      <PageHeader
        title={application?.name ?? 'Aplicação'}
        description={application ? `Slug: ${application.slug}` : 'Carregando detalhes...'}
      />
      <FeedbackAlerts success={success} error={error} />

      {!isSystemApp ? (
        <SectionCard
          title="Gerenciamento"
          subtitle="Operações disponíveis para esta aplicação."
          actions={
            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
              {canCreateClient ? (
                <Button startIcon={<AddIcon />} onClick={openClientDialog}>
                  Novo client OAuth
                </Button>
              ) : null}
              {canProvisionTenant ? (
                <Button startIcon={<BusinessOutlinedIcon />} onClick={openProvisionDialog}>
                  Provisionar tenant
                </Button>
              ) : null}
            </Stack>
          }
        >
          {!canCreateClient ? (
            <Alert severity="info">Para criar clients OAuth, use uma conta de administrador de plataforma.</Alert>
          ) : null}
          {!isPlatformAdministrator ? (
            <Alert severity="info">Apenas administradores de plataforma podem provisionar tenants por aplicação.</Alert>
          ) : null}
        </SectionCard>
      ) : (
        <Alert severity="info">Esta é uma aplicação de sistema gerida pela plataforma e não pode ser modificada.</Alert>
      )}

      <SectionCard title="Informações da aplicação">
        <Stack spacing={1.5}>
          <Box>
            <Typography variant="caption" color="text.secondary">
              ID
            </Typography>
            <Typography sx={{ fontFamily: 'monospace', fontSize: '0.875rem' }}>{application?.id ?? applicationId}</Typography>
          </Box>
          <Box>
            <Typography variant="caption" color="text.secondary">
              Nome
            </Typography>
            <Typography>{application?.name ?? '—'}</Typography>
          </Box>
          <Box>
            <Typography variant="caption" color="text.secondary">
              Slug
            </Typography>
            <Typography>{application?.slug ?? '—'}</Typography>
          </Box>
          <Box>
            <Typography variant="caption" color="text.secondary">
              Tipo
            </Typography>
            <Typography>{applicationTypeLabel(application?.type)}</Typography>
          </Box>
          {application?.isSystem ? (
            <Box>
              <Typography variant="caption" color="text.secondary">
                Classificação
              </Typography>
              <Typography>
                <Chip label="Sistema" size="small" color="default" variant="outlined" />
              </Typography>
            </Box>
          ) : null}
        </Stack>
      </SectionCard>

      {!isSystemApp && isPlatformAdministrator ? (
        <SectionCard
          title="Identidade visual (login)"
          subtitle="Cores e logo exibidos em /account/login e /account/register para usuários OAuth desta aplicação."
        >
          <Stack spacing={2}>
            <ApplicationBrandingFields
              value={brandingFields}
              onChange={setBrandingFields}
              savedLogoPath={application?.brandingLogoUrl}
              disabled={brandingSaving}
            />
            <Box>
              <Button variant="contained" disabled={brandingSaving} onClick={() => void handleSaveBranding()}>
                {brandingSaving ? 'Salvando...' : 'Salvar identidade visual'}
              </Button>
            </Box>
          </Stack>
        </SectionCard>
      ) : null}

      <SteppedFormDialog
        open={clientOpen}
        onClose={() => setClientOpen(false)}
        title="Novo client OAuth"
        description="Configure credenciais e parâmetros de autorização."
        steps={clientSteps}
        activeStep={clientStep}
        loading={loading}
        submitLabel="Criar client"
        onBack={() => setClientStep((step) => step - 1)}
        onNext={() => {
          if (clientStep === 1) {
            validateRedirectUrisInput()
          }
          setClientStep((step) => step + 1)
        }}
        onSubmit={handleCreateClient}
        disableNext={!clientStepValid}
        disableSubmit={!clientStepValid}
      >
        {clientStep === 0 ? (
          <FormSection title="Credenciais" description="Identificador e tipo do client.">
            <FormGrid>
              <FormGridItem>
                <TextField label="Client Id" value={clientId} onChange={(e) => setClientId(e.target.value)} required fullWidth />
              </FormGridItem>
              <FormGridItem>
                <TextField
                  select
                  label="Tipo do client"
                  value={clientType}
                  onChange={(e) => setClientType(e.target.value as ClientType)}
                  fullWidth
                >
                  {clientTypeOptions.map((option) => (
                    <MenuItem key={option.value} value={option.value}>
                      {option.label}
                    </MenuItem>
                  ))}
                </TextField>
              </FormGridItem>
              <FormGridItem xs={12} md={12}>
                <TextField
                  label="Hash do client secret"
                  value={clientSecretHash}
                  onChange={(e) => setClientSecretHash(e.target.value)}
                  fullWidth
                  slotProps={{
                    input: {
                      endAdornment: clientSecretHash ? (
                        <Tooltip title="Copiar">
                          <IconButton size="small" onClick={() => void copySecret()}>
                            <ContentCopyIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      ) : undefined,
                    },
                  }}
                />
              </FormGridItem>
            </FormGrid>
          </FormSection>
        ) : (
          <FormSection title="URIs e tokens" description="Redirect URIs, scopes e validade do access token.">
            <FormGrid>
              <FormGridItem xs={12} md={12}>
                <TextField
                  label="Redirect URIs"
                  value={redirectUris}
                  onChange={(e) => {
                    setRedirectUris(e.target.value)
                    setRedirectUriError(null)
                  }}
                  onBlur={() => validateRedirectUrisInput()}
                  required
                  fullWidth
                  multiline
                  minRows={2}
                  helperText="Uma URL absoluta (https://) por linha ou separadas por vírgula."
                  error={Boolean(redirectUriError)}
                />
                {redirectUriError ? (
                  <Typography variant="caption" color="error">
                    {redirectUriError}
                  </Typography>
                ) : null}
              </FormGridItem>
              <FormGridItem xs={12} md={12}>
                <Typography variant="subtitle2" gutterBottom>
                  Scopes permitidos
                </Typography>
                <FormGroup>
                  {OAUTH_SCOPE_OPTIONS.map((scope) => (
                    <CheckboxField
                      key={scope}
                      label={scope}
                      checked={selectedScopes.includes(scope)}
                      onCheckedChange={() => toggleScope(scope)}
                    />
                  ))}
                </FormGroup>
              </FormGridItem>
              <FormGridItem>
                <TextField
                  label="TTL do access token (segundos)"
                  value={accessTokenTtlSeconds}
                  onChange={(e) => setAccessTokenTtlSeconds(e.target.value)}
                  required
                  fullWidth
                />
              </FormGridItem>
            </FormGrid>
          </FormSection>
        )}
      </SteppedFormDialog>

      <SteppedFormDialog
        open={provisionOpen}
        onClose={() => setProvisionOpen(false)}
        title="Provisionar tenant"
        description="Onboarding de cliente SaaS após assinatura."
        steps={provisionSteps}
        activeStep={provisionStep}
        loading={loading}
        submitLabel="Provisionar"
        onBack={() => setProvisionStep((step) => step - 1)}
        onNext={() => setProvisionStep((step) => step + 1)}
        onSubmit={handleProvisionTenant}
        disableNext={!provisionStepValid}
        disableSubmit={!provisionStepValid}
      >
        {provisionStep === 0 ? (
          <FormSection title="Tenant" description="Nome e chave da organização provisionada.">
            <FormGrid>
              <FormGridItem>
                <TextField label="Nome do tenant" value={tenantName} onChange={(e) => setTenantName(e.target.value)} required fullWidth />
              </FormGridItem>
              <FormGridItem>
                <AvailabilityTextField
                  label="Chave do tenant"
                  value={tenantKey}
                  onChange={(e) => setTenantKey(e.target.value)}
                  required
                  fullWidth
                  availabilityStatus={tenantKeyAvailability}
                  availabilityMessages={tenantKeyAvailabilityMessages}
                  idleHelperText="Letras minúsculas, números e hífens"
                />
              </FormGridItem>
              <FormGridItem xs={12} md={12}>
                <UserPickerField
                  label="Administrador inicial"
                  selectedUser={provisionAdminUser}
                  onUserChange={setProvisionAdminUser}
                  inviteEmail={provisionAdminEmail}
                  onInviteEmailChange={setProvisionAdminEmail}
                  disabled={loading}
                />
              </FormGridItem>
            </FormGrid>
          </FormSection>
        ) : (
          <FormSection title="Metadados opcionais" description="Integração com billing ou CRM externo.">
            <FormGrid>
              <FormGridItem>
                <TextField
                  label="ID do cliente externo (opcional)"
                  value={externalCustomerId}
                  onChange={(e) => setExternalCustomerId(e.target.value)}
                  fullWidth
                />
              </FormGridItem>
              <FormGridItem>
                <TextField label="Plano (opcional)" value={planCode} onChange={(e) => setPlanCode(e.target.value)} fullWidth />
              </FormGridItem>
            </FormGrid>
          </FormSection>
        )}
      </SteppedFormDialog>
    </Stack>
  )
}
