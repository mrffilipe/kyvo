import { TextField, type TextFieldProps } from '@mui/material'
import type { AvailabilityStatus } from '../../hooks/useDebouncedAvailability'
import { getAvailabilityFieldState } from '../../utils/availabilityField'

export interface AvailabilityTextFieldProps extends Omit<TextFieldProps, 'error' | 'helperText'> {
  availabilityStatus: AvailabilityStatus
  availabilityMessages: {
    idle?: string
    checking?: string
    available: string
    unavailable: string
    invalid: string
  }
  idleHelperText?: string
}

export function AvailabilityTextField({
  availabilityStatus,
  availabilityMessages,
  idleHelperText,
  sx,
  ...props
}: AvailabilityTextFieldProps) {
  const field = getAvailabilityFieldState(availabilityStatus, {
    ...availabilityMessages,
    idle: idleHelperText ?? availabilityMessages.idle,
  })

  const helperSx =
    field.helperColor === 'success'
      ? { '& .MuiFormHelperText-root': { color: 'success.main' } }
      : field.helperColor === 'error'
        ? { '& .MuiFormHelperText-root': { color: 'error.main' } }
        : undefined

  return (
    <TextField
      {...props}
      error={field.error}
      helperText={field.helperText}
      sx={[helperSx, ...(Array.isArray(sx) ? sx : sx ? [sx] : [])]}
    />
  )
}
