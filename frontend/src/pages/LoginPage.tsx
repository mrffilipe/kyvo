import { Button, CircularProgress, Stack, Typography } from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import { useLoaderData, useRevalidator, useSearchParams } from 'react-router'
import { AuthLayout } from '../components/AuthLayout'
import { FeedbackAlerts } from '../components/ui'
import { env } from '../config'
import { bootstrapPlatform } from '../services/platformService'
import { redirectToOidcLogin } from '../services/oidcService'
import type { LoginLoaderData } from '../routes/loaders'
import { ensureAccessDeniedCookieLogout } from '../utils/authCleanup'
import { PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE } from '../utils/authStorage'
import { getApiErrorMessage } from '../utils/apiError'
import { useAuth } from '../contexts/AuthContext'

export function LoginPage() {
  const { requiresBootstrap } = useLoaderData() as LoginLoaderData
  const revalidator = useRevalidator()
  const { logoutLocal } = useAuth()
  const [searchParams] = useSearchParams()
  const accessDeniedMessage = useMemo(() => {
    if (searchParams.get('error') !== 'access_denied') {
      return null
    }
    return searchParams.get('error_description') ?? PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE
  }, [searchParams])
  const [loading, setLoading] = useState(!requiresBootstrap)
  const [error, setError] = useState<string | null>(accessDeniedMessage)
  const [pendingCookieLogout, setPendingCookieLogout] = useState(false)

  useEffect(() => {
    if (requiresBootstrap || searchParams.get('error') !== 'access_denied') {
      return
    }

    const loginUrl = `${window.location.origin}${window.location.pathname}${window.location.search}`
    if (ensureAccessDeniedCookieLogout(loginUrl)) {
      logoutLocal()
      setPendingCookieLogout(true)
    }
  }, [logoutLocal, requiresBootstrap, searchParams])

  useEffect(() => {
    if (requiresBootstrap || pendingCookieLogout) {
      return
    }

    let cancelled = false
    void (async () => {
      setLoading(true)
      try {
        await redirectToOidcLogin()
      } catch (loginError) {
        if (!cancelled) {
          setError(loginError instanceof Error ? loginError.message : 'Falha ao iniciar login.')
          setLoading(false)
        }
      }
    })()

    return () => {
      cancelled = true
    }
  }, [requiresBootstrap, pendingCookieLogout])

  useEffect(() => {
    if (!pendingCookieLogout) {
      return
    }

    let cancelled = false
    void (async () => {
      try {
        await redirectToOidcLogin()
      } catch (loginError) {
        if (!cancelled) {
          setError(loginError instanceof Error ? loginError.message : 'Falha ao iniciar login.')
          setLoading(false)
          setPendingCookieLogout(false)
        }
      }
    })()

    return () => {
      cancelled = true
    }
  }, [pendingCookieLogout])

  async function handleBootstrap(): Promise<void> {
    setLoading(true)
    setError(null)
    try {
      await bootstrapPlatform()
      await revalidator.revalidate()
    } catch (bootstrapError) {
      setError(getApiErrorMessage(bootstrapError))
    } finally {
      setLoading(false)
    }
  }

  if (requiresBootstrap) {
    return (
      <AuthLayout
        title="Primeira configuração"
        subtitle="A plataforma ainda não foi inicializada"
      >
        <Stack spacing={2.5}>
          <Typography variant="body2" color="text.secondary">
            As credenciais do administrador raiz são definidas no backend (variáveis{' '}
            <code>Bootstrap__AdminEmail</code> / <code>Bootstrap__AdminPassword</code> ou seção{' '}
            <code>Bootstrap</code> no appsettings). Clique abaixo para criar o usuário admin, o
            client OAuth <strong>{env.oauthClientId}</strong> e o provedor local.
          </Typography>
          <FeedbackAlerts error={error} />
          <Button
            variant="contained"
            size="large"
            disabled={loading || revalidator.state === 'loading'}
            onClick={() => void handleBootstrap()}
            sx={{ py: 1.25 }}
          >
            {loading ? 'Inicializando...' : 'Inicializar plataforma'}
          </Button>
        </Stack>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout title="Kyvo" subtitle={loading ? 'Redirecionando para o login…' : 'Preparando autenticação…'}>
      <Stack spacing={2.5} sx={{ alignItems: 'center', py: 1 }}>
        <CircularProgress size={32} />
        <FeedbackAlerts error={error} />
      </Stack>
    </AuthLayout>
  )
}
