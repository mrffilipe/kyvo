import type { AvailabilityStatus } from '../hooks/useDebouncedAvailability'

export interface AvailabilityFieldState {
  helperText?: string
  error: boolean
  helperColor?: 'success' | 'error'
}

export function getAvailabilityFieldState(
  status: AvailabilityStatus,
  messages: {
    idle?: string
    checking?: string
    available: string
    unavailable: string
    invalid: string
  },
): AvailabilityFieldState {
  switch (status) {
    case 'checking':
      return { helperText: messages.checking ?? 'Verificando disponibilidade…', error: false }
    case 'available':
      return { helperText: messages.available, error: false, helperColor: 'success' }
    case 'unavailable':
      return { helperText: messages.unavailable, error: true, helperColor: 'error' }
    case 'invalid':
      return { helperText: messages.invalid, error: true, helperColor: 'error' }
    default:
      return { helperText: messages.idle, error: false }
  }
}
