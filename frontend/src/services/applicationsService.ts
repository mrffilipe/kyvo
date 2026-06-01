import { api } from '../config'
import { normalizeApplication, normalizeApplicationBranding } from '../utils/apiMappers'
import { unwrapPagedResult } from '../utils/apiResponse'
import type {
  Application,
  ApplicationBranding,
  CreateApplicationBody,
  UpdateApplicationBrandingBody,
  CreateApplicationClientBody,
  CreateApplicationClientResponse,
  CreateApplicationResponse,
  ListApplicationsResponse,
  ProvisionApplicationTenantBody,
  ProvisionApplicationTenantResponse,
} from '../types'
import { apiPaths } from './httpPaths'

export interface ListApplicationsParams {
  page?: number
  pageSize?: number
}

export async function checkApplicationSlugAvailability(slug: string): Promise<boolean> {
  const encoded = encodeURIComponent(slug.trim().toLowerCase())
  const { data } = await api.get<{ available: boolean }>(
    `${apiPaths.applications}/slugs/${encoded}/availability`,
  )
  return data.available
}

export async function createApplication(body: CreateApplicationBody): Promise<CreateApplicationResponse> {
  const { data } = await api.post<CreateApplicationResponse>(apiPaths.applications, body)
  return data
}

export async function listApplications(
  params: ListApplicationsParams = {},
): Promise<ListApplicationsResponse> {
  const { data } = await api.get(apiPaths.applications, {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return unwrapPagedResult(data, normalizeApplication)
}

export async function getApplicationById(id: string): Promise<Application> {
  const { data } = await api.get(`${apiPaths.applications}/${id}`)
  return normalizeApplication(data)
}

export async function createApplicationClient(
  applicationId: string,
  body: CreateApplicationClientBody,
): Promise<CreateApplicationClientResponse> {
  const { data } = await api.post<CreateApplicationClientResponse>(
    `${apiPaths.applications}/${applicationId}/clients`,
    body,
  )
  return data
}

export async function getApplicationBranding(applicationId: string): Promise<ApplicationBranding> {
  const { data } = await api.get(`${apiPaths.applications}/${applicationId}/branding`)
  return normalizeApplicationBranding(data)
}

export async function updateApplicationBranding(
  applicationId: string,
  body: UpdateApplicationBrandingBody,
): Promise<void> {
  await api.patch(`${apiPaths.applications}/${applicationId}/branding`, body)
}

export async function uploadApplicationBrandingLogo(
  applicationId: string,
  file: File,
): Promise<string> {
  const formData = new FormData()
  formData.append('file', file)
  const { data } = await api.post<{ brandingLogoUrl: string }>(
    `${apiPaths.applications}/${applicationId}/branding/logo`,
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  )
  return data.brandingLogoUrl
}

export async function deleteApplicationBrandingLogo(applicationId: string): Promise<void> {
  await api.delete(`${apiPaths.applications}/${applicationId}/branding/logo`)
}

export async function persistApplicationBranding(
  applicationId: string,
  branding: {
    brandingEnabled: boolean
    brandingPrimaryColor: string
    brandingSecondaryColor: string
    brandingHeroTitle: string
    brandingHeroSubtitle: string
    logoFile: File | null
  },
): Promise<void> {
  if (!branding.brandingEnabled) {
    await updateApplicationBranding(applicationId, { brandingEnabled: false })
    return
  }

  await updateApplicationBranding(applicationId, {
    brandingEnabled: true,
    brandingPrimaryColor: branding.brandingPrimaryColor,
    brandingSecondaryColor: branding.brandingSecondaryColor,
    brandingHeroTitle: branding.brandingHeroTitle.trim() || null,
    brandingHeroSubtitle: branding.brandingHeroSubtitle.trim() || null,
  })

  if (branding.logoFile) {
    await uploadApplicationBrandingLogo(applicationId, branding.logoFile)
  }
}

export async function provisionApplicationTenant(
  applicationId: string,
  body: ProvisionApplicationTenantBody,
): Promise<ProvisionApplicationTenantResponse> {
  const { data } = await api.post<ProvisionApplicationTenantResponse>(
    `${apiPaths.applications}/${applicationId}/tenants/provision`,
    body,
  )
  return data
}
