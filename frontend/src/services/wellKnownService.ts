import { publicApi } from '../config'
import type { JwksResponse } from '../types'
import { apiPaths } from './httpPaths'

export async function getJwks(): Promise<JwksResponse> {
  const { data } = await publicApi.get<JwksResponse>(`${apiPaths.wellKnown}/jwks.json`)
  return data
}
