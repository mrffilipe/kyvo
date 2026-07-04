import { env } from '../config'

const HEX_COLOR = /^#[0-9a-fA-F]{6}$/

export function isValidBrandingColor(value: string): boolean {
  return HEX_COLOR.test(value.trim())
}

export function resolveBrandingLogoUrl(logoPath: string | null | undefined): string | null {
  if (!logoPath) {
    return null
  }

  if (logoPath.startsWith('http://') || logoPath.startsWith('https://')) {
    return logoPath
  }

  const base = env.apiBaseUrl.replace(/\/$/, '')
  const path = logoPath.startsWith('/') ? logoPath : `/${logoPath}`
  return `${base}${path}`
}

export const defaultBrandingPrimary = '#4f46e5'
export const defaultBrandingSecondary = '#7c3aed'
export const kyvoBrandIconUrl = '/brand/kyvo-icon.png'

export const defaultBrandingHeroTitle = 'Identidade para um ecossistema de apps.'
export const defaultBrandingHeroSubtitle =
  'Um lugar central para usuários, tenants e aplicações OAuth. Entre uma vez, acesse todos os produtos.'

export function resolveBrandingHeroTitle(custom: string | null | undefined): string {
  const trimmed = custom?.trim()
  return trimmed ? trimmed : defaultBrandingHeroTitle
}

export function resolveBrandingHeroSubtitle(custom: string | null | undefined): string {
  const trimmed = custom?.trim()
  return trimmed ? trimmed : defaultBrandingHeroSubtitle
}
