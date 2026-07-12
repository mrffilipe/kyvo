import axios from 'axios'
import { env } from './env'
import { refreshOidcTokens } from '../services/oidcService'
import { apiPaths } from '../services/httpPaths'
import { completeFailedPlatformLoginCleanup, clearClientAuthState } from '../utils/authCleanup'
import {
  applyTenantContext,
  getAuthSession,
  isPlatformAdministrator,
  resolveBearerToken,
  updateSessionFromOidcRefresh,
} from '../utils/authStorage'
import { parseApiBody } from '../utils/apiResponse'
import type { OidcTokenResponse, TenantContextResult } from '../types/oidc'

const baseURL = env.apiBaseUrl

const sharedConfig = {
  baseURL,
  timeout: env.apiTimeoutMs,
  headers: {
    Accept: 'application/json',
  },
  maxRedirects: 0,
}

export const publicApi = axios.create(sharedConfig)

export const api = axios.create(sharedConfig)

let refreshPromise: Promise<OidcTokenResponse> | null = null

async function reapplyTenantSwitch(platformAccessToken: string, tenantId: string): Promise<void> {
  const response = await axios.post<TenantContextResult>(
    `${baseURL}${apiPaths.auth}/switch-tenant`,
    { tenantId },
    {
      headers: {
        Accept: 'application/json',
        Authorization: `Bearer ${platformAccessToken}`,
      },
      timeout: env.apiTimeoutMs,
    },
  )
  applyTenantContext(parseApiBody(response.data) as TenantContextResult)
}

api.interceptors.request.use((config) => {
  const session = getAuthSession()
  if (session?.platformAccessToken) {
    config.headers.Authorization = `Bearer ${resolveBearerToken(session, config.url)}`
  }
  return config
})

api.interceptors.response.use(
  (response) => {
    if (typeof response.data === 'string') {
      const trimmed = response.data.trim()
      if (trimmed.startsWith('<!DOCTYPE') || trimmed.startsWith('<html')) {
        return Promise.reject(
          new Error('A API redirecionou para login. Faça logout e entre novamente.'),
        )
      }
    }

    response.data = parseApiBody(response.data)
    return response
  },
  async (error) => {
    const originalRequest = error.config as (typeof error.config & { _retry?: boolean }) | undefined
    const statusCode = error.response?.status

    if (statusCode === 302 || statusCode === 301) {
      return Promise.reject(
        new Error('A API redirecionou para login. Faça logout e entre novamente.'),
      )
    }

    if (statusCode === 403) {
      const session = getAuthSession()
      if (session?.platformAccessToken && !isPlatformAdministrator(session)) {
        completeFailedPlatformLoginCleanup()
        return Promise.reject(error)
      }
    }

    if (!originalRequest || statusCode !== 401 || originalRequest._retry) {
      return Promise.reject(error)
    }

    const session = getAuthSession()
    if (!session?.refreshToken) {
      clearClientAuthState()
      return Promise.reject(error)
    }

    originalRequest._retry = true

    try {
      if (!refreshPromise) {
        refreshPromise = refreshOidcTokens(session.refreshToken)
      }

      const refreshed = await refreshPromise
      const afterRefresh = updateSessionFromOidcRefresh(refreshed)
      if (!isPlatformAdministrator(afterRefresh)) {
        completeFailedPlatformLoginCleanup()
        return Promise.reject(new Error('Sessão sem permissão de administrador da plataforma.'))
      }

      if (afterRefresh.tenantId) {
        await reapplyTenantSwitch(afterRefresh.platformAccessToken, afterRefresh.tenantId)
      }

      const latest = getAuthSession()
      if (latest) {
        originalRequest.headers.Authorization = `Bearer ${resolveBearerToken(latest, originalRequest.url)}`
      }

      return await api.request(originalRequest)
    } catch (refreshError) {
      completeFailedPlatformLoginCleanup()
      return Promise.reject(refreshError)
    } finally {
      refreshPromise = null
    }
  },
)
