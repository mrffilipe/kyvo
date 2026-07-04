import { CircularProgress, Stack, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { AuthLayout } from '../components/AuthLayout'
import { FeedbackAlerts } from '../components/ui'
import { kyvoClient } from '../config/kyvoClient'
import { saveTokens } from '../utils/kyvoSession'
import { resolvePostLoginPath } from '../utils/postLoginRoute'

export function AuthCallbackPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const oauthError = searchParams.get('error')
  const oauthErrorMessage =
    oauthError !== null ? (searchParams.get('error_description') ?? oauthError) : null
  const [callbackError, setCallbackError] = useState<string | null>(null)
  const error = oauthErrorMessage ?? callbackError

  useEffect(() => {
    if (oauthErrorMessage !== null) {
      return
    }

    const code = searchParams.get('code')
    if (!code) {
      navigate('/login', { replace: true })
      return
    }

    if (!kyvoClient.oidc.tryAcquireCallbackLock()) return

    void (async () => {
      try {
        const tokens = await kyvoClient.oidc.handleCallback(code, searchParams.get('state'))
        kyvoClient.oidc.clearOidcRequest()
        saveTokens(tokens)

        await kyvoClient.users.getMe()

        navigate(await resolvePostLoginPath(), { replace: true })
      } catch (e) {
        kyvoClient.oidc.releaseCallbackLock()
        setCallbackError(e instanceof Error ? e.message : 'Falha no callback OIDC')
      }
    })()
  }, [navigate, oauthErrorMessage, searchParams])

  return (
    <AuthLayout title="Conectando" subtitle="Finalizando autenticação OpenID Connect">
      <Stack spacing={2} sx={{ alignItems: 'center', py: 2 }}>
        {error ? <FeedbackAlerts error={error} /> : <CircularProgress color="primary" />}
        {!error ? (
          <Typography variant="body2" color="text.secondary">
            Aguarde enquanto validamos sua sessão…
          </Typography>
        ) : null}
      </Stack>
    </AuthLayout>
  )
}
