import { Box, Button, Card, CardActionArea, CardContent, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { FlowLayout } from '../components/FlowLayout'
import { FeedbackAlerts, FormSection, PageHeader, SectionCard } from '../components/ui'
import { formSpacing } from '../theme/tokens'
import { PLANS } from '../types/crm'
import { setOnboardingDraft } from '../utils/kyvoSession'

export function OnboardingPage() {
  const navigate = useNavigate()
  const [companyName, setCompanyName] = useState('')
  const [planCode, setPlanCode] = useState<string>('professional')
  const [error, setError] = useState<string | null>(null)

  function handleContinue(): void {
    if (!companyName.trim()) {
      setError('Informe o nome da empresa.')
      return
    }
    setError(null)
    setOnboardingDraft(planCode, companyName.trim())
    navigate('/payment')
  }

  return (
    <FlowLayout>
      <Stack spacing={3}>
        <PageHeader
          title="Escolha seu plano"
          description="Após o pagamento (mock), vinculamos sua organização à aplicação Pulse CRM na Kyvo."
        />

        <FeedbackAlerts error={error} onDismissError={() => setError(null)} />

        <SectionCard title="Sua empresa">
          <FormSection title="Identificação" description="Nome que identificará sua organização no Pulse CRM.">
            <TextField
              label="Nome da empresa"
              value={companyName}
              onChange={(e) => setCompanyName(e.target.value)}
              placeholder="Acme Corp"
              fullWidth
            />
          </FormSection>
        </SectionCard>

        <SectionCard title="Planos disponíveis">
          <FormSection title="Seleção" description="Escolha o plano que deseja contratar.">
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' },
                gap: formSpacing.grid,
              }}
            >
              {PLANS.map((plan) => {
                const selected = planCode === plan.code
                return (
                  <Card
                    key={plan.code}
                    variant="outlined"
                    sx={{
                      borderWidth: 2,
                      borderColor: selected ? 'primary.main' : 'divider',
                      bgcolor: selected ? 'rgba(33, 150, 243, 0.08)' : 'background.paper',
                    }}
                  >
                    <CardActionArea onClick={() => setPlanCode(plan.code)}>
                      <CardContent>
                        <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                          {plan.name}
                        </Typography>
                        <Typography variant="h6" color="primary" sx={{ my: 0.5 }}>
                          {plan.price}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {plan.description}
                        </Typography>
                      </CardContent>
                    </CardActionArea>
                  </Card>
                )
              })}
            </Box>
          </FormSection>
        </SectionCard>

        <Button variant="contained" size="large" onClick={handleContinue} sx={{ alignSelf: 'flex-start' }}>
          Continuar para pagamento
        </Button>
      </Stack>
    </FlowLayout>
  )
}
