import { Button, Stack, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { FlowLayout } from '../components/FlowLayout'
import { FeedbackAlerts, FormSection, PageHeader, SectionCard } from '../components/ui'
import { kyvoClient } from '../config/kyvoClient'
import { completeOnboarding } from '../services/crmApi'
import { crmApiErrorMessage } from '../utils/crmErrors'
import { clearOnboardingDraft, getOnboardingDraft, updateTokens } from '../utils/kyvoSession'
import { PLANS } from '../types/crm'

export function PaymentPage() {
  const navigate = useNavigate()
  const draft = getOnboardingDraft()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!getOnboardingDraft()) {
      navigate('/onboarding', { replace: true })
    }
  }, [navigate])

  if (!draft) {
    return null
  }

  const plan = PLANS.find((p) => p.code === draft.planCode)

  async function handlePay(): Promise<void> {
    if (!draft) return
    setLoading(true)
    setError(null)
    try {
      const result = await completeOnboarding({
        companyName: draft.companyName,
        planCode: draft.planCode,
        paymentReference: `pay_mock_${Date.now()}`,
      })

      if (result.tokens?.access_token) {
        updateTokens(result.tokens)
      } else if (result.requiresTokenRefresh) {
        await kyvoClient.refreshAccessTokenWithTenant()
      }

      clearOnboardingDraft()
      navigate('/dashboard', { replace: true })
    } catch (e) {
      setError(crmApiErrorMessage(e) ?? (e instanceof Error ? e.message : 'Falha no pagamento/onboarding'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <FlowLayout>
      <Stack spacing={3}>
        <PageHeader title="Pagamento (mock)" description="Confirme para provisionar tenant e assinatura na Kyvo." />

        <FeedbackAlerts error={error} onDismissError={() => setError(null)} />

        <SectionCard title="Resumo">
          <FormSection title="Assinatura" description="Revise os dados antes de confirmar o pagamento simulado.">
            <Stack spacing={1}>
              <Typography variant="body2">
                <strong>Empresa:</strong> {draft.companyName}
              </Typography>
              <Typography variant="body2">
                <strong>Plano:</strong> {plan?.name ?? draft.planCode} — {plan?.price}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                O BFF chama <code>Kyvo.Client.Auth.SubscribeAsync</code> e devolve tokens OIDC quando disponíveis.
              </Typography>
            </Stack>
          </FormSection>
        </SectionCard>

        <Button variant="contained" size="large" disabled={loading} onClick={() => void handlePay()} sx={{ alignSelf: 'flex-start' }}>
          {loading ? 'Processando…' : 'Pagar e ativar (mock aprovado)'}
        </Button>
      </Stack>
    </FlowLayout>
  )
}
