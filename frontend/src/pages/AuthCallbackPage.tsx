import { CircularProgress, Stack, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import { AuthLayout } from '../components/AuthLayout'
import { FeedbackAlerts } from '../components/ui'
import { useAuth } from '../contexts/AuthContext'
import { useTenant } from '../contexts/TenantContext'
import { getMe } from '../services/usersService'
import {
  completeFailedPlatformLoginCleanup,
  clearClientAuthState,
} from '../utils/authCleanup'
import {
  enrichSessionFromUser,
  isPlatformAdministrator,
  PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE,
  saveSessionFromOidcTokens,
} from '../utils/authStorage'
import {
  clearOidcLoginRequest,
  consumeOidcState,
  consumePkceVerifier,
  redeemAuthorizationCode,
  releaseOidcCallbackLock,
  tryAcquireOidcCallbackLock,
} from '../services/oidcService'
import { getApiErrorMessage } from '../utils/apiError'

export function AuthCallbackPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { applyOidcLogin, logoutLocal } = useAuth()
  const { selectTenant } = useTenant()
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const code = searchParams.get('code')
    const state = searchParams.get('state')
    const oauthError = searchParams.get('error')

    if (oauthError) {
      const description = searchParams.get('error_description')
      const message =
        oauthError === 'access_denied'
          ? description ?? PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE
          : description ?? oauthError

      if (oauthError === 'access_denied') {
        logoutLocal()
        completeFailedPlatformLoginCleanup(message)
        return
      }

      clearClientAuthState()
      logoutLocal()
      setError(message)
      return
    }

    if (!code) {
      clearClientAuthState()
      navigate('/login', { replace: true })
      return
    }

    if (!tryAcquireOidcCallbackLock()) {
      return
    }

    void (async () => {
      try {
        consumeOidcState(state)
        const verifier = consumePkceVerifier()
        const tokens = await redeemAuthorizationCode(code, verifier)
        clearOidcLoginRequest()

        const session = saveSessionFromOidcTokens(tokens)
        if (!isPlatformAdministrator(session)) {
          logoutLocal()
          completeFailedPlatformLoginCleanup(PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE)
          return
        }

        applyOidcLogin(tokens, session.tenants)

        try {
          const profile = await getMe()
          const enriched = enrichSessionFromUser(profile)
          applyOidcLogin(tokens, enriched.tenants)
          const preferredTenantId =
            enriched.tenantId ?? profile.memberships[0]?.tenantId ?? null
          if (preferredTenantId) {
            selectTenant(preferredTenantId)
          }
        } catch {
          /* claims do JWT já permitem navegar; perfil completa depois */
        }

        navigate('/', { replace: true })
      } catch (callbackError) {
        releaseOidcCallbackLock()
        clearClientAuthState()
        logoutLocal()
        setError(getApiErrorMessage(callbackError))
      }
    })()
  }, [applyOidcLogin, logoutLocal, navigate, searchParams, selectTenant])

  if (error) {
    return (
      <AuthLayout title="Falha no login" subtitle="Não foi possível concluir a autenticação">
        <Stack spacing={2} sx={{ width: '100%' }}>
          <FeedbackAlerts error={error} />
        </Stack>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout title="Conectando…" subtitle="Finalizando login OIDC">
      <Stack spacing={2} sx={{ alignItems: 'center' }}>
        <CircularProgress size={32} />
        <Typography variant="body2" color="text.secondary">
          Trocando authorization code por tokens…
        </Typography>
      </Stack>
    </AuthLayout>
  )
}
