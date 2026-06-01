import AddIcon from '@mui/icons-material/Add'
import { Button, Chip, MenuItem, Stack, TableCell, TableRow, TextField } from '@mui/material'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router'
import {
  ApplicationBrandingFields,
  validateBrandingFields,
  type ApplicationBrandingFieldsValue,
} from '../components/applications/ApplicationBrandingFields'
import {
  AvailabilityTextField,
  DataTable,
  FeedbackAlerts,
  FormGrid,
  FormGridItem,
  FormSection,
  PageHeader,
  SectionCard,
  SteppedFormDialog,
} from '../components/ui'
import { useAuth } from '../contexts/AuthContext'
import { useDebouncedAvailability } from '../hooks/useDebouncedAvailability'
import { checkApplicationSlugAvailability, createApplication, listApplications, persistApplicationBranding } from '../services'
import { ApplicationType, type Application } from '../types'
import { defaultBrandingPrimary, defaultBrandingSecondary } from '../utils/brandingUtils'
import { getApiErrorMessage } from '../utils/apiError'
import { applicationTypeLabel } from '../utils/enumLabels'

const initialBrandingFields: ApplicationBrandingFieldsValue = {
  brandingEnabled: false,
  brandingPrimaryColor: defaultBrandingPrimary,
  brandingSecondaryColor: defaultBrandingSecondary,
  brandingHeroTitle: '',
  brandingHeroSubtitle: '',
  logoFile: null,
}

const typeOptions: Array<{ label: string; value: ApplicationType }> = [
  { label: 'Web', value: ApplicationType.Web },
  { label: 'Mobile', value: ApplicationType.Mobile },
  { label: 'Backend', value: ApplicationType.Backend },
]

const createSteps = ['Identificação', 'Identidade visual'] as const

function isValidApplicationSlug(value: string): boolean {
  const normalized = value.trim().toLowerCase()
  return normalized.length >= 2 && /^[a-z0-9][a-z0-9-]*$/.test(normalized)
}

const slugAvailabilityMessages = {
  checking: 'Verificando disponibilidade…',
  available: 'Slug disponível',
  unavailable: 'Slug já está em uso',
  invalid: 'Use letras minúsculas, números e hífens',
}

export function ApplicationsPage() {
  const { platformRoles } = useAuth()
  const isPlatformAdministrator = platformRoles.includes('plat_admin')
  const [items, setItems] = useState<Application[]>([])
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [createOpen, setCreateOpen] = useState(false)
  const [createStep, setCreateStep] = useState(0)
  const [loading, setLoading] = useState(false)

  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')
  const [type, setType] = useState<ApplicationType>(ApplicationType.Web)
  const [brandingFields, setBrandingFields] = useState<ApplicationBrandingFieldsValue>(initialBrandingFields)

  const checkSlugAvailable = useCallback(
    (value: string) => checkApplicationSlugAvailability(value.trim().toLowerCase()),
    [],
  )
  const slugAvailability = useDebouncedAvailability(slug, checkSlugAvailable, isValidApplicationSlug)

  useEffect(() => {
    void loadApplications()
  }, [])

  async function loadApplications(): Promise<void> {
    setError(null)
    try {
      const result = await listApplications({ page: 1, pageSize: 100 })
      setItems(result.items)
    } catch (loadError) {
      setError(getApiErrorMessage(loadError))
    }
  }

  function openCreateDialog(): void {
    setName('')
    setSlug('')
    setType(ApplicationType.Web)
    setBrandingFields(initialBrandingFields)
    setCreateStep(0)
    setCreateOpen(true)
  }

  const step0Valid = useMemo(
    () =>
      Boolean(name.trim() && slug.trim()) &&
      slugAvailability !== 'unavailable' &&
      slugAvailability !== 'invalid' &&
      slugAvailability !== 'checking',
    [name, slug, slugAvailability],
  )

  async function handleCreate(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()

    const brandingValidation = validateBrandingFields(brandingFields)
    if (brandingValidation) {
      setError(brandingValidation)
      return
    }

    setLoading(true)
    setError(null)
    setSuccess(null)
    try {
      const created = await createApplication({
        name,
        slug: slug.trim().toLowerCase(),
        type,
      })
      if (brandingFields.brandingEnabled) {
        await persistApplicationBranding(created.id, brandingFields)
      }
      setSuccess(`Aplicação criada: ${created.id}`)
      setCreateOpen(false)
      await loadApplications()
    } catch (createError) {
      setError(getApiErrorMessage(createError))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Aplicações"
        description="Gerencie aplicações OAuth registradas na plataforma."
        actions={
          isPlatformAdministrator ? (
            <Button startIcon={<AddIcon />} onClick={openCreateDialog}>
              Nova aplicação
            </Button>
          ) : null
        }
      />
      <FeedbackAlerts success={success} error={error} />

      <SectionCard title="Aplicações cadastradas">
        <DataTable
          columns={[
            { id: 'id', label: 'Id', minWidth: 120 },
            { id: 'name', label: 'Nome' },
            { id: 'slug', label: 'Slug' },
            { id: 'type', label: 'Tipo' },
            { id: 'actions', label: 'Ações', align: 'right' },
          ]}
          rows={items.map((item) => (
            <TableRow key={item.id} hover>
              <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>{item.id}</TableCell>
              <TableCell>
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                  <span>{item.name}</span>
                  {item.isSystem ? (
                    <Chip label="Sistema" size="small" color="default" variant="outlined" />
                  ) : null}
                </Stack>
              </TableCell>
              <TableCell>{item.slug}</TableCell>
              <TableCell>{applicationTypeLabel(item.type)}</TableCell>
              <TableCell align="right">
                <Button component={Link} to={`/applications/${item.id}`} size="small">
                  Detalhes
                </Button>
              </TableCell>
            </TableRow>
          ))}
          emptyDescription="Nenhuma aplicação cadastrada ainda."
        />
      </SectionCard>

      <SteppedFormDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title="Nova aplicação"
        description="Registre uma aplicação OAuth na plataforma."
        steps={createSteps}
        activeStep={createStep}
        loading={loading}
        submitLabel="Criar"
        onBack={() => setCreateStep((step) => step - 1)}
        onNext={() => setCreateStep((step) => step + 1)}
        onSubmit={handleCreate}
        disableNext={createStep === 0 && !step0Valid}
        disableSubmit={createStep === 0 ? !step0Valid : false}
      >
        {createStep === 0 ? (
          <FormSection title="Identificação" description="Nome público e slug único da aplicação.">
            <FormGrid>
              <FormGridItem>
                <TextField label="Nome" value={name} onChange={(event) => setName(event.target.value)} required fullWidth />
              </FormGridItem>
              <FormGridItem>
                <AvailabilityTextField
                  label="Slug"
                  value={slug}
                  onChange={(event) => setSlug(event.target.value)}
                  required
                  fullWidth
                  availabilityStatus={slugAvailability}
                  availabilityMessages={slugAvailabilityMessages}
                />
              </FormGridItem>
              <FormGridItem>
                <TextField
                  select
                  label="Tipo"
                  value={type}
                  onChange={(event) => setType(event.target.value as ApplicationType)}
                  fullWidth
                >
                  {typeOptions.map((option) => (
                    <MenuItem key={option.value} value={option.value}>
                      {option.label}
                    </MenuItem>
                  ))}
                </TextField>
              </FormGridItem>
            </FormGrid>
          </FormSection>
        ) : isPlatformAdministrator ? (
          <FormSection
            title="Identidade visual"
            description="Opcional: personalize login e registro no IdP para usuários desta aplicação."
          >
            <ApplicationBrandingFields value={brandingFields} onChange={setBrandingFields} />
          </FormSection>
        ) : null}
      </SteppedFormDialog>
    </Stack>
  )
}
