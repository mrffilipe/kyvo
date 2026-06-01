import type { HttpClient } from '../api/httpClient.js'
import type { ApiPaths } from '../api/paths.js'
import type {
  PagedResult,
  UpdateUserProfileBody,
  UserDto,
  UserMembershipDto,
} from '../types/api.js'

export function createUsersResource(http: HttpClient, paths: ApiPaths) {
  return {
    getMe(): Promise<UserDto> {
      return http.request('GET', `${paths.users}/me`)
    },

    updateMe(body: UpdateUserProfileBody): Promise<void> {
      return http.request('PATCH', `${paths.users}/me`, { body })
    },

    listMyMemberships(page = 1, pageSize = 20): Promise<PagedResult<UserMembershipDto>> {
      return http.request('GET', `${paths.users}/me/memberships`, { params: { page, pageSize } })
    },
  }
}
