import { Button, FormControlLabel, Stack, Switch, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { FormGrid, FormGridItem } from '../ui/FormGrid'
import {
  defaultBrandingHeroSubtitle,
  defaultBrandingHeroTitle,
  defaultBrandingPrimary,
  defaultBrandingSecondary,
  isValidBrandingColor,
} from '../../utils/brandingUtils'
import { ApplicationBrandingPreview } from './ApplicationBrandingPreview'
import { BrandingColorPicker } from './BrandingColorPicker'

export interface ApplicationBrandingFieldsValue {
  brandingEnabled: boolean
  brandingPrimaryColor: string
  brandingSecondaryColor: string
  brandingHeroTitle: string
  brandingHeroSubtitle: string
  logoFile: File | null
}

const heroTitleMaxLength = 200
const heroSubtitleMaxLength = 500

interface ApplicationBrandingFieldsProps {
  value: ApplicationBrandingFieldsValue
  onChange: (value: ApplicationBrandingFieldsValue) => void
  savedLogoPath?: string | null
  disabled?: boolean
}

export function ApplicationBrandingFields({
  value,
  onChange,
  savedLogoPath = null,
  disabled = false,
}: ApplicationBrandingFieldsProps) {
  const [logoPreviewUrl, setLogoPreviewUrl] = useState<string | null>(null)

  useEffect(() => {
    if (!value.logoFile) {
      setLogoPreviewUrl(null)
      return
    }

    const objectUrl = URL.createObjectURL(value.logoFile)
    setLogoPreviewUrl(objectUrl)
    return () => URL.revokeObjectURL(objectUrl)
  }, [value.logoFile])

  return (
    <Stack spacing={2.5}>
      <FormControlLabel
        control={
          <Switch
            checked={value.brandingEnabled}
            disabled={disabled}
            onChange={(event) =>
              onChange({
                ...value,
                brandingEnabled: event.target.checked,
                brandingPrimaryColor: event.target.checked
                  ? value.brandingPrimaryColor || defaultBrandingPrimary
                  : value.brandingPrimaryColor,
                brandingSecondaryColor: event.target.checked
                  ? value.brandingSecondaryColor || defaultBrandingSecondary
                  : value.brandingSecondaryColor,
              })
            }
          />
        }
        label="Personalizar tela de login"
      />

      {value.brandingEnabled ? (
        <>
          <Typography variant="body2" color="text.secondary">
            Cores, textos do painel lateral e logo aparecem em /account/login e /account/register quando o usuário
            entra via OAuth desta application.
          </Typography>

          <FormGrid>
            <FormGridItem xs={12} md={6}>
              <TextField
                label="Título do painel lateral"
                value={value.brandingHeroTitle}
                disabled={disabled}
                onChange={(event) => onChange({ ...value, brandingHeroTitle: event.target.value })}
                fullWidth
                placeholder={defaultBrandingHeroTitle}
                slotProps={{ htmlInput: { maxLength: heroTitleMaxLength } }}
                helperText={`Deixe em branco para usar o texto padrão Kyvo. Máx. ${heroTitleMaxLength} caracteres.`}
              />
            </FormGridItem>
            <FormGridItem xs={12} md={6}>
              <TextField
                label="Mensagem do painel lateral"
                value={value.brandingHeroSubtitle}
                disabled={disabled}
                onChange={(event) => onChange({ ...value, brandingHeroSubtitle: event.target.value })}
                fullWidth
                multiline
                minRows={3}
                placeholder={defaultBrandingHeroSubtitle}
                slotProps={{ htmlInput: { maxLength: heroSubtitleMaxLength } }}
                helperText={`Deixe em branco para usar o texto padrão Kyvo. Máx. ${heroSubtitleMaxLength} caracteres.`}
              />
            </FormGridItem>
            <FormGridItem xs={12} md={6}>
              <BrandingColorPicker
                id="branding-primary-color"
                label="Cor primária"
                value={value.brandingPrimaryColor}
                fallback={defaultBrandingPrimary}
                disabled={disabled}
                onChange={(hex) => onChange({ ...value, brandingPrimaryColor: hex })}
              />
            </FormGridItem>
            <FormGridItem xs={12} md={6}>
              <BrandingColorPicker
                id="branding-secondary-color"
                label="Cor secundária"
                value={value.brandingSecondaryColor}
                fallback={defaultBrandingSecondary}
                disabled={disabled}
                onChange={(hex) => onChange({ ...value, brandingSecondaryColor: hex })}
              />
            </FormGridItem>
          </FormGrid>

          <Stack spacing={1}>
            <Typography variant="body2" color="text.secondary">
              Logo (PNG, JPEG, WebP ou SVG — máx. 512 KB)
            </Typography>
            <Button variant="outlined" component="label" disabled={disabled} sx={{ alignSelf: 'flex-start' }}>
              Escolher arquivo
              <input
                type="file"
                hidden
                accept="image/png,image/jpeg,image/webp,image/svg+xml"
                onChange={(event) => {
                  const file = event.target.files?.[0] ?? null
                  onChange({ ...value, logoFile: file })
                }}
              />
            </Button>
            {value.logoFile ? (
              <Typography variant="caption" color="text.secondary">
                {value.logoFile.name}
              </Typography>
            ) : null}
          </Stack>

          <ApplicationBrandingPreview
            brandingEnabled={value.brandingEnabled}
            primaryColor={value.brandingPrimaryColor}
            secondaryColor={value.brandingSecondaryColor}
            heroTitle={value.brandingHeroTitle}
            heroSubtitle={value.brandingHeroSubtitle}
            logoPreviewUrl={logoPreviewUrl}
            savedLogoPath={savedLogoPath}
          />
        </>
      ) : null}
    </Stack>
  )
}

export function validateBrandingFields(value: ApplicationBrandingFieldsValue): string | null {
  if (!value.brandingEnabled) {
    return null
  }

  if (!isValidBrandingColor(value.brandingPrimaryColor)) {
    return 'Selecione uma cor primária.'
  }

  if (!isValidBrandingColor(value.brandingSecondaryColor)) {
    return 'Selecione uma cor secundária.'
  }

  if (value.brandingHeroTitle.trim().length > heroTitleMaxLength) {
    return `O título do painel deve ter no máximo ${heroTitleMaxLength} caracteres.`
  }

  if (value.brandingHeroSubtitle.trim().length > heroSubtitleMaxLength) {
    return `A mensagem do painel deve ter no máximo ${heroSubtitleMaxLength} caracteres.`
  }

  return null
}
