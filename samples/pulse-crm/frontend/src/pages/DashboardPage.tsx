import { Box, CircularProgress, Stack, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import type { OidcUserInfoResponse, UserDto } from '@kyvo-client/client'
import { kyvoClient } from '../config/kyvoClient'
import { PageHeader, SectionCard, FeedbackAlerts } from '../components/ui'
import { getMe } from '../services/crmApi'
import type { MeResponse } from '../types/crm'

export function DashboardPage() {
  const [me, setMe] = useState<MeResponse | null>(null)
  const [kyvoUser, setKyvoUser] = useState<UserDto | null>(null)
  const [userInfo, setUserInfo] = useState<OidcUserInfoResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void (async () => {
      try {
        const [profile, kyvoProfile] = await Promise.all([getMe(), kyvoClient.users.getMe()])
        setMe(profile)
        setKyvoUser(kyvoProfile)

        const token = kyvoClient.getAccessToken()
        if (token) {
          setUserInfo(await kyvoClient.oidc.fetchUserInfo(token))
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Falha ao carregar perfil')
      }
    })()
  }, [])

  if (error) {
    return (
      <Stack spacing={2}>
        <PageHeader title="Dashboard" />
        <FeedbackAlerts error={error} />
      </Stack>
    )
  }

  if (!me) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    )
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Dashboard"
        description="Pulse CRM (API local) + perfil Kyvo via @kyvo-client/client (REST e UserInfo OIDC)."
      />

      <SectionCard title="Perfil CRM">
        <Stack spacing={1}>
          <Typography variant="body2">
            <strong>E-mail:</strong> {me.email ?? '—'}
          </Typography>
          <Typography variant="body2">
            <strong>Tenant (efetivo):</strong> {me.tenantId ?? '—'}
          </Typography>
          <Typography variant="body2">
            <strong>Assinatura CRM:</strong>{' '}
            {me.hasSubscription ? me.subscription?.companyName : 'Pendente'}
          </Typography>
        </Stack>
      </SectionCard>

      {kyvoUser ? (
        <SectionCard title="Kyvo Users/me" subtitle="GET /api/v1/Users/me via @kyvo-client/client">
          <Stack spacing={1}>
            <Typography variant="body2">
              <strong>Display name:</strong> {kyvoUser.displayName}
            </Typography>
            <Typography variant="body2">
              <strong>Memberships:</strong> {kyvoUser.memberships.length}
            </Typography>
            <Box
              component="pre"
              sx={{
                m: 0,
                p: 2,
                borderRadius: 1,
                bgcolor: 'action.hover',
                overflow: 'auto',
                fontSize: '0.8rem',
              }}
            >
              {JSON.stringify(kyvoUser, null, 2)}
            </Box>
          </Stack>
        </SectionCard>
      ) : null}

      {userInfo ? (
        <SectionCard title="OIDC UserInfo" subtitle="kyvoClient.oidc.fetchUserInfo">
          <Box
            component="pre"
            sx={{
              m: 0,
              p: 2,
              borderRadius: 1,
              bgcolor: 'action.hover',
              overflow: 'auto',
              fontSize: '0.8rem',
            }}
          >
            {JSON.stringify(userInfo, null, 2)}
          </Box>
        </SectionCard>
      ) : null}
    </Stack>
  )
}
