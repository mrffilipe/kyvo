import { Box, Container, Stack } from '@mui/material'
import type { PropsWithChildren } from 'react'
import { ThemeModeToggle } from './ThemeModeToggle'
import { PulseBrand } from './PulseBrand'
import { layout, formSpacing } from '../theme'

/** Centered layout for onboarding / payment steps (authenticated, no sidebar). */
export function FlowLayout({ children }: PropsWithChildren) {
  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default', py: 3 }}>
      <Box sx={{ position: 'absolute', top: 16, right: 16, zIndex: 1 }}>
        <ThemeModeToggle />
      </Box>
      <Container maxWidth="md">
        <Stack spacing={formSpacing.stack + 0.5}>
          <PulseBrand to="/dashboard" />
          <Box sx={{ maxWidth: layout.flowMaxWidth, mx: 'auto', width: '100%' }}>{children}</Box>
        </Stack>
      </Container>
    </Box>
  )
}
