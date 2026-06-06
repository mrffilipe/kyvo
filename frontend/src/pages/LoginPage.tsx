import { Button, CircularProgress, Stack, Typography } from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import { useLoaderData, useSearchParams } from 'react-router'
import { AuthLayout } from '../components/AuthLayout'
import { FeedbackAlerts } from '../components/ui'
import { redirectToOidcLogin } from '../services/oidcService'
import type { LoginLoaderData } from '../routes/loaders'
import { ensureAccessDeniedCookieLogout } from '../utils/authCleanup'
import { PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE } from '../utils/authStorage'
import { useAuth } from '../contexts/AuthContext'

export function LoginPage() {
  const { requiresBootstrap } = useLoaderData() as LoginLoaderData
  const { logoutLocal } = useAuth()
  const [searchParams] = useSearchParams()
  const isAccessDenied = searchParams.get('error') === 'access_denied'
  const accessDeniedMessage = useMemo(() => {
    if (!isAccessDenied) {
      return null
    }
    return searchParams.get('error_description') ?? PLATFORM_ADMIN_ACCESS_DENIED_MESSAGE
  }, [isAccessDenied, searchParams])
  const [loading, setLoading] = useState(!requiresBootstrap && !isAccessDenied)
  const [error, setError] = useState<string | null>(accessDeniedMessage)
  const [pendingCookieLogout, setPendingCookieLogout] = useState(false)

  useEffect(() => {
    if (requiresBootstrap || !isAccessDenied) {
      return
    }

    const loginUrl = `${window.location.origin}${window.location.pathname}${window.location.search}`
    if (ensureAccessDeniedCookieLogout(loginUrl)) {
      logoutLocal()
      setPendingCookieLogout(true)
    }
  }, [isAccessDenied, logoutLocal, requiresBootstrap, searchParams])

  useEffect(() => {
    if (requiresBootstrap || pendingCookieLogout || isAccessDenied) {
      if (isAccessDenied && !pendingCookieLogout) {
        setLoading(false)
      }
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
  }, [isAccessDenied, pendingCookieLogout, requiresBootstrap])

  async function handleRetryLogin(): Promise<void> {
    setLoading(true)
    setError(null)
    try {
      await redirectToOidcLogin()
    } catch (loginError) {
      setError(loginError instanceof Error ? loginError.message : 'Falha ao iniciar login.')
      setLoading(false)
    }
  }

  if (requiresBootstrap) {
    return (
      <AuthLayout
        title="Plataforma não inicializada"
        subtitle="Aguardando configuração no backend"
      >
        <Stack spacing={2.5}>
          <Typography variant="body2" color="text.secondary">
            A plataforma ainda não foi inicializada. Configure{' '}
            <code>Bootstrap__AdminEmail</code> e <code>Bootstrap__AdminPassword</code> no backend
            (ou a seção <code>Bootstrap</code> no appsettings) e reinicie a API.
          </Typography>
          <FeedbackAlerts error={error} />
        </Stack>
      </AuthLayout>
    )
  }

  if (isAccessDenied && !pendingCookieLogout) {
    return (
      <AuthLayout
        title="Sem permissão para este console"
        subtitle="Este painel é exclusivo para administradores da plataforma Kyvo."
      >
        <Stack spacing={2.5}>
          <FeedbackAlerts error={error ?? accessDeniedMessage} />
          <Typography variant="body2" color="text.secondary">
            Se você acredita que deveria ter acesso, entre com a conta de administrador da plataforma
            ou solicite permissão ao responsável pelo ambiente.
          </Typography>
          <Button
            variant="contained"
            size="large"
            disabled={loading}
            onClick={() => void handleRetryLogin()}
            sx={{ py: 1.25 }}
          >
            {loading ? 'Redirecionando...' : 'Tentar com outra conta'}
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
