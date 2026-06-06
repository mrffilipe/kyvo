import { publicApi } from '../config/axios'
import { apiPaths } from './httpPaths'
import type { PlatformStatus } from '../types'

export async function getPlatformStatus(): Promise<PlatformStatus> {
  const { data } = await publicApi.get<PlatformStatus>(`${apiPaths.platform}/status`)
  return data
}
