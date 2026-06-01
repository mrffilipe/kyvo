import { useEffect, useState } from 'react'

const DEBOUNCE_MS = 400

export type AvailabilityStatus = 'idle' | 'checking' | 'available' | 'unavailable' | 'invalid'

export function useDebouncedAvailability(
  value: string,
  checkAvailable: (normalized: string) => Promise<boolean>,
  isValidFormat: (value: string) => boolean,
): AvailabilityStatus {
  const [status, setStatus] = useState<AvailabilityStatus>('idle')

  useEffect(() => {
    const trimmed = value.trim()
    if (!trimmed) {
      setStatus('idle')
      return
    }

    if (!isValidFormat(trimmed)) {
      setStatus('invalid')
      return
    }

    setStatus('checking')
    const handle = setTimeout(() => {
      void checkAvailable(trimmed)
        .then((available) => {
          setStatus(available ? 'available' : 'unavailable')
        })
        .catch(() => {
          setStatus('idle')
        })
    }, DEBOUNCE_MS)

    return () => clearTimeout(handle)
  }, [value, checkAvailable, isValidFormat])

  return status
}
