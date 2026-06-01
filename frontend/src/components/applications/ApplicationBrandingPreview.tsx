import DarkModeOutlinedIcon from '@mui/icons-material/DarkModeOutlined'
import LightModeOutlinedIcon from '@mui/icons-material/LightModeOutlined'
import { Box, IconButton, Stack, Tooltip, Typography } from '@mui/material'
import { useMemo, useState } from 'react'
import {
  defaultBrandingPrimary,
  defaultBrandingSecondary,
  kyvoBrandIconUrl,
  resolveBrandingHeroSubtitle,
  resolveBrandingHeroTitle,
  resolveBrandingLogoUrl,
} from '../../utils/brandingUtils'
import './accountPreview.css'

export interface ApplicationBrandingPreviewProps {
  brandingEnabled: boolean
  primaryColor: string
  secondaryColor: string
  heroTitle: string
  heroSubtitle: string
  logoPreviewUrl: string | null
  savedLogoPath: string | null
}

export function ApplicationBrandingPreview({
  brandingEnabled,
  primaryColor,
  secondaryColor,
  heroTitle,
  heroSubtitle,
  logoPreviewUrl,
  savedLogoPath,
}: ApplicationBrandingPreviewProps) {
  const [mode, setMode] = useState<'light' | 'dark'>('light')

  const primary = brandingEnabled && primaryColor ? primaryColor : defaultBrandingPrimary
  const secondary = brandingEnabled && secondaryColor ? secondaryColor : defaultBrandingSecondary
  const displayHeroTitle = resolveBrandingHeroTitle(brandingEnabled ? heroTitle : null)
  const displayHeroSubtitle = resolveBrandingHeroSubtitle(brandingEnabled ? heroSubtitle : null)

  const logoSrc = useMemo(() => {
    if (logoPreviewUrl) {
      return logoPreviewUrl
    }

    const resolved = resolveBrandingLogoUrl(savedLogoPath)
    if (resolved) {
      return resolved
    }

    return kyvoBrandIconUrl
  }, [logoPreviewUrl, savedLogoPath])

  const style = {
    '--preview-primary': primary,
    '--preview-primary-hover': primary,
    '--preview-hero-from': primary,
    '--preview-hero-to': secondary,
  } as React.CSSProperties

  return (
    <Stack spacing={1}>
      <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography variant="subtitle2" color="text.secondary">
          Pré-visualização da tela de login
        </Typography>
        <Tooltip title={mode === 'light' ? 'Modo escuro' : 'Modo claro'}>
          <IconButton
            size="small"
            onClick={() => setMode(mode === 'light' ? 'dark' : 'light')}
            aria-label="Alternar tema da pré-visualização"
          >
            {mode === 'light' ? <DarkModeOutlinedIcon fontSize="small" /> : <LightModeOutlinedIcon fontSize="small" />}
          </IconButton>
        </Tooltip>
      </Stack>
      <Box className="account-preview-root" data-theme={mode} style={style} sx={{ width: '100%' }}>
        <div className="account-preview-shell">
          <aside className="account-preview-hero" aria-hidden="true">
            <img className="account-preview-hero-logo" src={logoSrc} alt="" />
            <h2 className="account-preview-hero-title">{displayHeroTitle}</h2>
            <p className="account-preview-hero-subtitle">{displayHeroSubtitle}</p>
          </aside>
          <div className="account-preview-panel">
            <div className="account-preview-card">
              <img className="account-preview-card-logo" src={logoSrc} alt="" />
              <h3>Bem-vindo de volta</h3>
              <p>Entre para continuar nas suas aplicações.</p>
              <button type="button" className="account-preview-btn">
                Entrar
              </button>
            </div>
          </div>
        </div>
      </Box>
    </Stack>
  )
}
