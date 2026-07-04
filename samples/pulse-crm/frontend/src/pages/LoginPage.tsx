import { CircularProgress, Stack } from '@mui/material'
import { useEffect, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { AuthLayout } from '../components/AuthLayout'
import { FeedbackAlerts } from '../components/ui'
import { kyvoClient } from '../config/kyvoClient'
import { crmApiErrorMessage } from '../utils/crmErrors'
import { isLoggedIn } from '../utils/kyvoSession'
import { resolvePostLoginPath } from '../utils/postLoginRoute'

export function LoginPage() {
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [redirectTo, setRedirectTo] = useState<string | null>(null)

  useEffect(() => {
    if (!isLoggedIn()) {
      let cancelled = false
      void kyvoClient.oidc
        .signInRedirect()
        .catch((e) => {
          if (!cancelled) {
            setError(e instanceof Error ? e.message : 'Falha ao iniciar login')
            setLoading(false)
          }
        })
      return () => {
        cancelled = true
      }
    }

    void resolvePostLoginPath()
      .then(setRedirectTo)
      .catch((e) => {
        setError(
          crmApiErrorMessage(e) ??
            'Não foi possível validar a sessão com a API do Pulse CRM. Confira se Kyvo e a API usam o mesmo issuer (VITE_KYVO_AUTHORITY / Kyvo:Authority).',
        )
        setLoading(false)
      })
  }, [])

  if (redirectTo) {
    return <Navigate to={redirectTo} replace />
  }

  return (
    <AuthLayout title="Pulse CRM" subtitle={loading ? 'Redirecionando para o Kyvo…' : 'Preparando autenticação…'}>
      <Stack spacing={2.5} sx={{ alignItems: 'center', py: 1 }}>
        <CircularProgress size={32} />
        <FeedbackAlerts error={error} />
      </Stack>
    </AuthLayout>
  )
}
