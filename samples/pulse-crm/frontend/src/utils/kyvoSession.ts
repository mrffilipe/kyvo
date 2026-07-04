import type { OidcTokenResponse } from '@kyvo-client/client'
import { kyvoClient } from '../config/kyvoClient'

/** Persist OIDC tokens via `@kyvo-client/client` SessionManager. */
export function saveTokens(tokens: OidcTokenResponse): void {
  kyvoClient.session.saveFromTokens(tokens)
}

export function updateTokens(tokens: OidcTokenResponse): void {
  kyvoClient.session.updateAccessToken(tokens)
}

export function clearTokens(): void {
  kyvoClient.session.clear()
}

export function isLoggedIn(): boolean {
  return Boolean(kyvoClient.getAccessToken())
}

const ONBOARDING_PLAN_KEY = 'pulsecrm.onboarding.plan'
const ONBOARDING_COMPANY_KEY = 'pulsecrm.onboarding.company'

export function setOnboardingDraft(planCode: string, companyName: string): void {
  sessionStorage.setItem(ONBOARDING_PLAN_KEY, planCode)
  sessionStorage.setItem(ONBOARDING_COMPANY_KEY, companyName)
}

export function getOnboardingDraft(): { planCode: string; companyName: string } | null {
  const planCode = sessionStorage.getItem(ONBOARDING_PLAN_KEY)
  const companyName = sessionStorage.getItem(ONBOARDING_COMPANY_KEY)
  if (!planCode || !companyName) return null
  return { planCode, companyName }
}

export function clearOnboardingDraft(): void {
  sessionStorage.removeItem(ONBOARDING_PLAN_KEY)
  sessionStorage.removeItem(ONBOARDING_COMPANY_KEY)
}
