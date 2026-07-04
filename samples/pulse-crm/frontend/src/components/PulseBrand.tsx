import { Box, Link, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { pulseBrand } from '../theme/tokens'

interface PulseBrandProps {
  to?: string
  compact?: boolean
}

export function PulseBrand({ to, compact = false }: PulseBrandProps) {
  const inner = (
    <Box
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 1,
        color: 'inherit',
      }}
    >
      <Box
        sx={{
          width: compact ? 32 : 40,
          height: compact ? 32 : 40,
          borderRadius: 1.5,
          background: `linear-gradient(145deg, ${pulseBrand.primary}, ${pulseBrand.secondary})`,
          flexShrink: 0,
        }}
      />
      <Typography
        variant={compact ? 'subtitle1' : 'h6'}
        component="span"
        sx={{
          fontWeight: 700,
          letterSpacing: '-0.02em',
          background: `linear-gradient(90deg, ${pulseBrand.primary}, ${pulseBrand.secondary})`,
          WebkitBackgroundClip: 'text',
          WebkitTextFillColor: 'transparent',
          backgroundClip: 'text',
        }}
      >
        Pulse CRM
      </Typography>
    </Box>
  )

  if (to) {
    return (
      <Link component={RouterLink} to={to} underline="none" color="inherit">
        {inner}
      </Link>
    )
  }

  return inner
}
