import { Box, Paper, Stack, Typography } from '@mui/material'
import type { PropsWithChildren } from 'react'
import { useThemeMode } from '../hooks/useThemeMode'
import { ThemeModeToggle } from './ThemeModeToggle'
import { getAuthBackground, layout, pulseBrand, radius } from '../theme'

interface AuthLayoutProps extends PropsWithChildren {
  maxWidth?: number
  title?: string
  subtitle?: string
}

export function AuthLayout({
  children,
  maxWidth = layout.authMaxWidth,
  title = 'Pulse CRM',
  subtitle = 'Sample SaaS integrado à Kyvo (OIDC + PKCE)',
}: AuthLayoutProps) {
  const { mode } = useThemeMode()

  return (
    <Box
      sx={{
        position: 'relative',
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        px: 2,
        py: 4,
        bgcolor: getAuthBackground(mode),
      }}
    >
      <Box sx={{ position: 'absolute', top: 16, right: 16, zIndex: 1 }}>
        <ThemeModeToggle />
      </Box>
      <Box sx={{ width: '100%', maxWidth, mx: 'auto' }}>
        <Stack spacing={2.5} sx={{ mb: 3, alignItems: 'center', textAlign: 'center' }}>
          <Box
            sx={{
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: '#ffffff',
              borderRadius: `${radius.md}px`,
              border: 1,
              borderColor: 'divider',
              p: 1,
              boxShadow: '0 4px 20px rgba(15, 18, 30, 0.12)',
            }}
          >
            <Box
              sx={{
                width: 56,
                height: 56,
                borderRadius: `${radius.md}px`,
                background: `linear-gradient(145deg, ${pulseBrand.primary}, ${pulseBrand.secondary})`,
              }}
            />
          </Box>
          <Stack spacing={0.5} sx={{ width: '100%' }}>
            <Typography variant="h5" component="h1">
              {title}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {subtitle}
            </Typography>
          </Stack>
        </Stack>
        <Paper sx={{ p: { xs: 2.5, sm: 3.5 } }}>{children}</Paper>
      </Box>
    </Box>
  )
}
