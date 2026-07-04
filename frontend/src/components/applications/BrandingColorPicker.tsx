import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import {
  Box,
  IconButton,
  InputAdornment,
  Popover,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useMemo, useRef, useState, type PointerEvent as ReactPointerEvent } from 'react'
import { isValidBrandingColor } from '../../utils/brandingUtils'
import { BRANDING_PRESET_COLORS, hexToHsv, hsvToHex, normalizeHex } from '../../utils/colorUtils'

interface BrandingColorPickerProps {
  id: string
  label: string
  value: string
  fallback: string
  onChange: (hex: string) => void
  disabled?: boolean
}

export function BrandingColorPicker({
  id,
  label,
  value,
  fallback,
  onChange,
  disabled = false,
}: BrandingColorPickerProps) {
  const anchorRef = useRef<HTMLDivElement>(null)
  const svRef = useRef<HTMLDivElement>(null)
  const [open, setOpen] = useState(false)
  const [hexInput, setHexInput] = useState('')

  const resolvedHex = isValidBrandingColor(value) ? value : fallback
  const hsv = useMemo(() => hexToHsv(resolvedHex) ?? { h: 243, s: 76, v: 90 }, [resolvedHex])

  function openPicker(): void {
    if (disabled) {
      return
    }
    setHexInput(resolvedHex.toUpperCase())
    setOpen(true)
  }

  function applyHex(nextHex: string): void {
    const normalized = normalizeHex(nextHex)
    if (normalized) {
      onChange(normalized)
      setHexInput(normalized.toUpperCase())
    }
  }

  function updateFromHsv(next: { h: number; s: number; v: number }): void {
    const nextHex = hsvToHex(next.h, next.s, next.v)
    onChange(nextHex)
    setHexInput(nextHex.toUpperCase())
  }

  function handleSvPointer(event: ReactPointerEvent<HTMLDivElement>): void {
    const element = svRef.current
    if (!element) {
      return
    }

    const updateFromClient = (clientX: number, clientY: number) => {
      const rect = element.getBoundingClientRect()
      const x = Math.max(0, Math.min(rect.width, clientX - rect.left))
      const y = Math.max(0, Math.min(rect.height, clientY - rect.top))
      updateFromHsv({
        h: hsv.h,
        s: (x / rect.width) * 100,
        v: 100 - (y / rect.height) * 100,
      })
    }

    updateFromClient(event.clientX, event.clientY)

    function onMove(moveEvent: PointerEvent): void {
      updateFromClient(moveEvent.clientX, moveEvent.clientY)
    }

    function onUp(): void {
      window.removeEventListener('pointermove', onMove)
      window.removeEventListener('pointerup', onUp)
    }

    window.addEventListener('pointermove', onMove)
    window.addEventListener('pointerup', onUp)
  }

  return (
    <>
      <Box ref={anchorRef}>
        <TextField
          id={id}
          label={label}
          value={resolvedHex.toUpperCase()}
          fullWidth
          disabled={disabled}
          onClick={openPicker}
          slotProps={{
            input: {
              readOnly: true,
              sx: { cursor: disabled ? 'not-allowed' : 'pointer' },
              startAdornment: (
                <InputAdornment position="start">
                  <Box
                    sx={{
                      width: 28,
                      height: 28,
                      borderRadius: 1,
                      bgcolor: resolvedHex,
                      border: 1,
                      borderColor: 'divider',
                      boxShadow: 'inset 0 0 0 1px rgba(255,255,255,0.12)',
                    }}
                  />
                </InputAdornment>
              ),
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    size="small"
                    edge="end"
                    disabled={disabled}
                    onClick={(event) => {
                      event.stopPropagation()
                      openPicker()
                    }}
                    aria-label={`Abrir seletor de ${label}`}
                  >
                    <ExpandMoreIcon fontSize="small" />
                  </IconButton>
                </InputAdornment>
              ),
            },
          }}
        />
      </Box>

      <Popover
        open={open}
        anchorEl={anchorRef.current}
        onClose={() => setOpen(false)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
        slotProps={{
          paper: {
            sx: { p: 2, width: 280, mt: 0.5 },
          },
        }}
      >
        <Stack spacing={2}>
          <Box
            ref={svRef}
            onPointerDown={handleSvPointer}
            sx={{
              position: 'relative',
              width: '100%',
              height: 128,
              borderRadius: 1.5,
              overflow: 'hidden',
              cursor: 'crosshair',
              touchAction: 'none',
              background: `
                linear-gradient(to top, #000, transparent),
                linear-gradient(to right, #fff, hsl(${hsv.h}, 100%, 50%))
              `,
            }}
          >
            <Box
              sx={{
                position: 'absolute',
                left: `${hsv.s}%`,
                top: `${100 - hsv.v}%`,
                width: 14,
                height: 14,
                borderRadius: '50%',
                border: '2px solid #fff',
                boxShadow: '0 0 0 1px rgba(0,0,0,0.35)',
                transform: 'translate(-50%, -50%)',
                pointerEvents: 'none',
              }}
            />
          </Box>

          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
            <Box
              sx={{
                width: 36,
                height: 36,
                borderRadius: 1,
                bgcolor: resolvedHex,
                border: 1,
                borderColor: 'divider',
                flexShrink: 0,
              }}
            />
            <Box
              component="input"
              type="range"
              min={0}
              max={360}
              value={Math.round(hsv.h)}
              onChange={(event) =>
                updateFromHsv({ ...hsv, h: Number(event.target.value) })
              }
              aria-label={`Matiz de ${label}`}
              sx={{
                flex: 1,
                height: 12,
                appearance: 'none',
                borderRadius: 999,
                background:
                  'linear-gradient(to right, #f00, #ff0, #0f0, #0ff, #00f, #f0f, #f00)',
                cursor: 'pointer',
                '&::-webkit-slider-thumb': {
                  appearance: 'none',
                  width: 16,
                  height: 16,
                  borderRadius: '50%',
                  bgcolor: '#fff',
                  border: '2px solid',
                  borderColor: 'divider',
                  boxShadow: 1,
                },
                '&::-moz-range-thumb': {
                  width: 16,
                  height: 16,
                  borderRadius: '50%',
                  bgcolor: '#fff',
                  border: '2px solid',
                  borderColor: 'divider',
                  boxShadow: 1,
                },
              }}
            />
          </Stack>

          <Stack spacing={0.75}>
            <Typography variant="caption" color="text.secondary">
              Cores sugeridas
            </Typography>
            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(6, 1fr)', gap: 0.75 }}>
              {BRANDING_PRESET_COLORS.map((preset) => (
                <Box
                  key={preset}
                  component="button"
                  type="button"
                  aria-label={`Usar ${preset}`}
                  onClick={() => applyHex(preset)}
                  sx={{
                    width: '100%',
                    aspectRatio: '1',
                    borderRadius: 1,
                    bgcolor: preset,
                    border: 2,
                    borderColor: preset === resolvedHex ? 'primary.main' : 'divider',
                    cursor: 'pointer',
                    p: 0,
                    transition: 'transform 0.15s ease, border-color 0.15s ease',
                    '&:hover': {
                      transform: 'scale(1.06)',
                    },
                  }}
                />
              ))}
            </Box>
          </Stack>

          <TextField
            label="Hex"
            size="small"
            value={hexInput}
            onChange={(event) => {
              const next = event.target.value
              setHexInput(next.startsWith('#') ? next.toUpperCase() : `#${next.toUpperCase()}`)
            }}
            onBlur={() => applyHex(hexInput)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                applyHex(hexInput)
              }
            }}
            slotProps={{
              htmlInput: {
                maxLength: 7,
                spellCheck: false,
              },
            }}
          />
        </Stack>
      </Popover>
    </>
  )
}
