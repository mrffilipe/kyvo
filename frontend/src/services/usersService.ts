import { api } from '../config'
import type { ListUserMembershipsResponse, SearchUsersResponse, UpdateMeBody, User } from '../types'
import { normalizeUser, normalizeUserMembership, normalizeUserPickerItem } from '../utils/apiMappers'
import { unwrapPagedResult } from '../utils/apiResponse'
import { apiPaths } from './httpPaths'

export async function getMe(): Promise<User> {
  const { data } = await api.get(`${apiPaths.users}/me`)
  return normalizeUser(data)
}

export async function updateMe(body: UpdateMeBody): Promise<void> {
  await api.patch(`${apiPaths.users}/me`, body)
}

export interface ListUserMembershipsParams {
  page?: number
  pageSize?: number
}

export interface SearchUsersParams {
  search: string
  page?: number
  pageSize?: number
}

export async function searchUsers(params: SearchUsersParams): Promise<SearchUsersResponse> {
  const { data } = await api.get(apiPaths.users, {
    params: {
      search: params.search,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return unwrapPagedResult(data, normalizeUserPickerItem)
}

export async function listMyMemberships(
  params: ListUserMembershipsParams = {},
): Promise<ListUserMembershipsResponse> {
  const { data } = await api.get(`${apiPaths.users}/me/memberships`, {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return unwrapPagedResult(data, normalizeUserMembership)
}
