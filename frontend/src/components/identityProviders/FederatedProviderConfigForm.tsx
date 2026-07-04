import Visibility from '@mui/icons-material/Visibility'
import VisibilityOff from '@mui/icons-material/VisibilityOff'
import { IconButton, InputAdornment, TextField } from '@mui/material'
import { useState } from 'react'
import { FormGrid, FormGridItem } from '../ui'
import type { FederatedConfigFieldValues } from '../../utils/federatedIdpConfig'
import { IdentityProviderType, type IdentityProviderType as IdentityProviderTypeValue } from '../../types'

interface FederatedProviderConfigFormProps {
  providerType: IdentityProviderTypeValue
  values: FederatedConfigFieldValues
  onChange: (values: FederatedConfigFieldValues) => void
  mode: 'create' | 'update'
}

/**
 * Client credentials for OpenIddict.Client-based federation. Google/Microsoft/GitHub only need
 * ClientId/ClientSecret (OpenIddict.Client.WebIntegration knows their discovery endpoint already);
 * GenericOidc additionally needs the provider's own Issuer for discovery.
 */
export function FederatedProviderConfigForm({
  providerType,
  values,
  onChange,
  mode,
}: FederatedProviderConfigFormProps) {
  const [showSecret, setShowSecret] = useState(false)
  const requiresIssuer = providerType === IdentityProviderType.GenericOidc

  return (
    <FormGrid>
      <FormGridItem xs={12} md={6}>
        <TextField
          label="Client ID"
          value={values.clientId}
          onChange={(event) => onChange({ ...values, clientId: event.target.value })}
          required={mode === 'create'}
          fullWidth
          helperText="Emitido pelo console do provedor ao registrar o app OAuth."
        />
      </FormGridItem>
      <FormGridItem xs={12} md={6}>
        <TextField
          label="Client Secret"
          type={showSecret ? 'text' : 'password'}
          value={values.clientSecret}
          onChange={(event) => onChange({ ...values, clientSecret: event.target.value })}
          required={mode === 'create'}
          fullWidth
          helperText={mode === 'update' ? 'Deixe em branco para manter o segredo atual.' : undefined}
          slotProps={{
            input: {
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    aria-label={showSecret ? 'Ocultar segredo' : 'Mostrar segredo'}
                    onClick={() => setShowSecret((visible) => !visible)}
                    edge="end"
                  >
                    {showSecret ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
                  </IconButton>
                </InputAdornment>
              ),
            },
          }}
        />
      </FormGridItem>
      {requiresIssuer ? (
        <FormGridItem xs={12}>
          <TextField
            label="Issuer"
            value={values.issuer}
            onChange={(event) => onChange({ ...values, issuer: event.target.value })}
            required={mode === 'create'}
            fullWidth
            placeholder="https://minha-instancia.exemplo.com"
            helperText="URL base do provedor OIDC; usada para descobrir /.well-known/openid-configuration."
          />
        </FormGridItem>
      ) : null}
    </FormGrid>
  )
}
