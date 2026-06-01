import { requiresOnboarding } from '@kyvo-client/client'
import { kyvoClient } from '../config/kyvoClient'
import { getMe } from '../services/crmApi'

/** CRM subscription wins; JWT `tid` via SDK when subscription is still missing. */
export async function resolvePostLoginPath(): Promise<'/dashboard' | '/onboarding'> {
  const me = await getMe()
  if (me.hasSubscription) {
    return '/dashboard'
  }

  const token = kyvoClient.getAccessToken()
  if (token && requiresOnboarding(token)) {
    return '/onboarding'
  }

  return '/onboarding'
}
