// Default values must stay in sync with backend/Kyvo.Domain/Constants/PlatformDefaults.cs
// (admin console client id and redirect URI) and backend/Kyvo.API/appsettings.Development.json
// (issuer/api base url) so the admin SPA runs without an .env file in local development.
//
// Production Docker image: VITE_API_BASE_URL and VITE_OAUTH_REDIRECT_URI are left empty at build time;
// in the browser the SPA uses window.location.origin (same public host as the API via external proxy).
const ENV_DEFAULTS = {
  VITE_API_BASE_URL: 'https://localhost:5101',
  VITE_API_TIMEOUT_MS: '30000',
  VITE_OAUTH_CLIENT_ID: 'platform-admin-web',
  VITE_OAUTH_REDIRECT_URI: 'http://localhost:3000/auth/callback',
} as const

type EnvKey = keyof typeof ENV_DEFAULTS

function isUnset(value: string | undefined): boolean {
  return value === undefined || String(value).trim() === ''
}

function getBakedEnv(name: EnvKey): string | undefined {
  const value = (import.meta.env as Record<string, string | undefined>)[name]
  return isUnset(value) ? undefined : String(value)
}

function resolveApiBaseUrl(): string {
  const baked = getBakedEnv('VITE_API_BASE_URL')
  if (baked !== undefined) {
    return baked.replace(/\/$/, '')
  }

  // Production image is built with an empty VITE_API_BASE_URL; API and SPA share the public host.
  if (import.meta.env.PROD && typeof window !== 'undefined') {
    return window.location.origin
  }

  return ENV_DEFAULTS.VITE_API_BASE_URL
}

function resolveOAuthRedirectUri(): string {
  const baked = getBakedEnv('VITE_OAUTH_REDIRECT_URI')
  if (baked !== undefined) {
    return baked.replace(/\/$/, '')
  }

  if (import.meta.env.PROD && typeof window !== 'undefined') {
    return `${window.location.origin}/auth/callback`
  }

  return ENV_DEFAULTS.VITE_OAUTH_REDIRECT_URI
}

function getEnvWithDefault(name: Exclude<EnvKey, 'VITE_API_BASE_URL' | 'VITE_OAUTH_REDIRECT_URI'>): string {
  const baked = getBakedEnv(name)
  if (baked !== undefined) {
    return baked
  }

  return ENV_DEFAULTS[name]
}

function getPositiveNumberFromEnv(name: 'VITE_API_TIMEOUT_MS'): number {
  const raw = getEnvWithDefault(name)
  const parsed = Number(raw)
  if (!Number.isFinite(parsed) || parsed <= 0) {
    throw new Error(`Environment variable ${name} must be a positive number. Received: ${raw}`)
  }

  return parsed
}

export const env = {
  apiBaseUrl: resolveApiBaseUrl(),
  apiTimeoutMs: getPositiveNumberFromEnv('VITE_API_TIMEOUT_MS'),
  oauthClientId: getEnvWithDefault('VITE_OAUTH_CLIENT_ID'),
  oauthRedirectUri: resolveOAuthRedirectUri(),
}
