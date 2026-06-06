import { redirect } from 'react-router'
import { getPlatformStatus } from '../services/platformService'
import { clearClientAuthState } from '../utils/authCleanup'
import {
  getAuthSession,
  isPlatformAdministrator,
  PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE,
} from '../utils/authStorage'
import { getSelectedTenantId } from '../utils/tenantStorage'
import type { LoaderFunctionArgs } from 'react-router'

export interface LoginLoaderData {
  requiresBootstrap: boolean
}

function redirectToLoginWithAccessDenied(): never {
  clearClientAuthState()
  const description = encodeURIComponent(PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE)
  throw redirect(`/login?error=access_denied&error_description=${description}`)
}

export async function requireAuthLoader({ request }: LoaderFunctionArgs): Promise<null> {
  const status = await getPlatformStatus()
  if (status.requiresBootstrap) {
    if (getAuthSession()?.accessToken) {
      clearClientAuthState()
    }
    throw redirect('/login')
  }

  const session = getAuthSession()
  if (!session?.accessToken) {
    const url = new URL(request.url)
    const returnUrl = encodeURIComponent(url.pathname + url.search)
    throw redirect(`/login?returnUrl=${returnUrl}`)
  }

  if (!isPlatformAdministrator(session)) {
    redirectToLoginWithAccessDenied()
  }

  return null
}

export async function loginLoader({ request }: LoaderFunctionArgs): Promise<LoginLoaderData> {
  const status = await getPlatformStatus()

  if (status.requiresBootstrap) {
    if (getAuthSession()?.accessToken) {
      clearClientAuthState()
    }
    return { requiresBootstrap: true }
  }

  const session = getAuthSession()
  if (session?.accessToken) {
    if (!isPlatformAdministrator(session)) {
      clearClientAuthState()
      return { requiresBootstrap: false }
    }

    const url = new URL(request.url)
    const returnUrl = url.searchParams.get('returnUrl') ?? '/'
    throw redirect(returnUrl)
  }

  return { requiresBootstrap: false }
}

export async function requireTenantLoader(args: LoaderFunctionArgs): Promise<null> {
  await requireAuthLoader(args)

  const tenantId = getSelectedTenantId()
  if (!tenantId) {
    throw redirect('/tenants')
  }

  return null
}
