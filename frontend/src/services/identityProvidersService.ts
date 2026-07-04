import { api } from '../config'
import type {
  AddIdentityProviderBody,
  AddIdentityProviderResult,
  IdentityProviderDto,
  UpdateIdentityProviderBody,
} from '../types'
import { apiPaths } from './httpPaths'

import { normalizeIdpAliasInput } from '../utils/idpAliasValidation'

export async function checkIdentityProviderAliasAvailability(alias: string): Promise<boolean> {
  const encoded = encodeURIComponent(normalizeIdpAliasInput(alias))
  const { data } = await api.get<{ available: boolean }>(
    `${apiPaths.identityProviders}/aliases/${encoded}/availability`,
  )
  return data.available
}

export async function listIdentityProviders(): Promise<IdentityProviderDto[]> {
  const { data } = await api.get<IdentityProviderDto[]>(apiPaths.identityProviders)
  return data
}

export async function addIdentityProvider(body: AddIdentityProviderBody): Promise<AddIdentityProviderResult> {
  const { data } = await api.post<AddIdentityProviderResult>(apiPaths.identityProviders, body)
  return data
}

export async function updateIdentityProvider(id: string, body: UpdateIdentityProviderBody): Promise<void> {
  await api.patch(`${apiPaths.identityProviders}/${id}`, body)
}

export async function enableIdentityProvider(id: string): Promise<void> {
  await api.post(`${apiPaths.identityProviders}/${id}/enable`)
}

export async function disableIdentityProvider(id: string): Promise<void> {
  await api.post(`${apiPaths.identityProviders}/${id}/disable`)
}
