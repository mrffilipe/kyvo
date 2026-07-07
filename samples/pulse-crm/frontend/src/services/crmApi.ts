import axios, { type AxiosError } from 'axios'
import { hasTenant } from '@kyvo-client/client'
import { env } from '../config/env'
import { kyvoClient } from '../config/kyvoClient'
import type { Contact, MeResponse, OnboardingCompleteResponse } from '../types/crm'
import { updateTokens } from '../utils/kyvoSession'

export const crmApi = axios.create({
  baseURL: env.crmApiUrl,
  headers: { Accept: 'application/json' },
})

crmApi.interceptors.request.use((config) => {
  const token = kyvoClient.getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let refreshPromise: ReturnType<typeof kyvoClient.oidc.refresh> | null = null
let tenantSwitchPromise: ReturnType<typeof kyvoClient.switchTenant> | null = null

function isMissingTenantError(error: AxiosError): boolean {
  const message = (error.response?.data as { message?: string } | undefined)?.message
  return (
    error.response?.status === 400 &&
    typeof message === 'string' &&
    message.toLowerCase().includes('tenant')
  )
}

async function switchToSubscriptionTenant(): Promise<void> {
  const me = await crmApi.get<MeResponse>('/api/me').then((r) => r.data)
  const tenantId = me.subscription?.tenantId
  if (!tenantId) {
    throw new Error('Assinatura sem tenant. Conclua o onboarding.')
  }

  if (!tenantSwitchPromise) {
    tenantSwitchPromise = kyvoClient.switchTenant(tenantId)
  }
  await tenantSwitchPromise
}

crmApi.interceptors.response.use(
  (r) => r,
  async (error: AxiosError) => {
    const original = error.config as typeof error.config & {
      _retry?: boolean
      _tenantRetry?: boolean
    }
    if (!original) {
      return Promise.reject(error)
    }

    if (isMissingTenantError(error) && !original._tenantRetry) {
      original._tenantRetry = true
      try {
        await switchToSubscriptionTenant()
        const token = kyvoClient.getAccessToken()
        if (token) {
          original.headers.Authorization = `Bearer ${token}`
        }
        return crmApi.request(original)
      } catch {
        return Promise.reject(error)
      } finally {
        tenantSwitchPromise = null
      }
    }

    if (error.response?.status !== 401 || original._retry) {
      return Promise.reject(error)
    }

    const session = kyvoClient.session.getSession()
    if (!session?.platform.refreshToken) return Promise.reject(error)

    original._retry = true
    try {
      if (!refreshPromise) refreshPromise = kyvoClient.oidc.refresh(session.platform.refreshToken)
      const tokens = await refreshPromise
      updateTokens(tokens)
      original.headers.Authorization = `Bearer ${tokens.access_token}`
      return crmApi.request(original)
    } finally {
      refreshPromise = null
    }
  },
)

export async function ensureTenantAccessToken(): Promise<void> {
  const token = kyvoClient.getAccessToken()
  if (!token) {
    throw new Error('Sessão ausente. Faça login novamente.')
  }
  if (hasTenant(token) || kyvoClient.session.hasActiveTenantToken()) {
    return
  }

  await switchToSubscriptionTenant()
}

export async function getMe(): Promise<MeResponse> {
  const { data } = await crmApi.get<MeResponse>('/api/me')
  return data
}

export async function completeOnboarding(body: {
  companyName: string
  planCode: string
  paymentReference?: string
}): Promise<OnboardingCompleteResponse> {
  const { data } = await crmApi.post<OnboardingCompleteResponse>('/api/onboarding/complete', body)
  return data
}

export async function listContacts(): Promise<Contact[]> {
  const { data } = await crmApi.get<Contact[]>('/api/contacts')
  return data
}

export async function createContact(body: {
  name: string
  email: string
  phone?: string
}): Promise<Contact> {
  const { data } = await crmApi.post<Contact>('/api/contacts', body)
  return data
}

export async function updateContact(
  id: string,
  body: { name: string; email: string; phone?: string },
): Promise<Contact> {
  const { data } = await crmApi.put<Contact>(`/api/contacts/${id}`, body)
  return data
}

export async function deleteContact(id: string): Promise<void> {
  await crmApi.delete(`/api/contacts/${id}`)
}
