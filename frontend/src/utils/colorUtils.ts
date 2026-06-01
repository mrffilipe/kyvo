const HEX_COLOR = /^#[0-9a-fA-F]{6}$/

export function normalizeHex(value: string): string | null {
  const trimmed = value.trim()
  if (HEX_COLOR.test(trimmed)) {
    return trimmed.toLowerCase()
  }

  const withHash = trimmed.startsWith('#') ? trimmed : `#${trimmed}`
  if (HEX_COLOR.test(withHash)) {
    return withHash.toLowerCase()
  }

  return null
}

export function hexToRgb(hex: string): { r: number; g: number; b: number } | null {
  const normalized = normalizeHex(hex)
  if (!normalized) {
    return null
  }

  const value = normalized.slice(1)
  return {
    r: Number.parseInt(value.slice(0, 2), 16),
    g: Number.parseInt(value.slice(2, 4), 16),
    b: Number.parseInt(value.slice(4, 6), 16),
  }
}

export function rgbToHex(r: number, g: number, b: number): string {
  const clamp = (channel: number) => Math.max(0, Math.min(255, Math.round(channel)))
  return `#${[clamp(r), clamp(g), clamp(b)]
    .map((channel) => channel.toString(16).padStart(2, '0'))
    .join('')}`
}

export function hexToHsv(hex: string): { h: number; s: number; v: number } | null {
  const rgb = hexToRgb(hex)
  if (!rgb) {
    return null
  }

  const r = rgb.r / 255
  const g = rgb.g / 255
  const b = rgb.b / 255
  const max = Math.max(r, g, b)
  const min = Math.min(r, g, b)
  const delta = max - min

  let h = 0
  if (delta !== 0) {
    if (max === r) {
      h = ((g - b) / delta) % 6
    } else if (max === g) {
      h = (b - r) / delta + 2
    } else {
      h = (r - g) / delta + 4
    }
    h *= 60
    if (h < 0) {
      h += 360
    }
  }

  const s = max === 0 ? 0 : delta / max
  const v = max

  return { h, s: s * 100, v: v * 100 }
}

export function hsvToHex(h: number, s: number, v: number): string {
  const hue = ((h % 360) + 360) % 360
  const saturation = Math.max(0, Math.min(100, s)) / 100
  const value = Math.max(0, Math.min(100, v)) / 100

  const c = value * saturation
  const x = c * (1 - Math.abs(((hue / 60) % 2) - 1))
  const m = value - c

  let r = 0
  let g = 0
  let b = 0

  if (hue < 60) {
    r = c
    g = x
  } else if (hue < 120) {
    r = x
    g = c
  } else if (hue < 180) {
    g = c
    b = x
  } else if (hue < 240) {
    g = x
    b = c
  } else if (hue < 300) {
    r = x
    b = c
  } else {
    r = c
    b = x
  }

  return rgbToHex((r + m) * 255, (g + m) * 255, (b + m) * 255)
}

export const BRANDING_PRESET_COLORS = [
  '#4f46e5',
  '#4338ca',
  '#6366f1',
  '#818cf8',
  '#7c3aed',
  '#6d28d9',
  '#a78bfa',
  '#2563eb',
  '#0891b2',
  '#059669',
  '#0b0d12',
  '#4b5060',
] as const
