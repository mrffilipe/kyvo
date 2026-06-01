import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined'
import Visibility from '@mui/icons-material/Visibility'
import VisibilityOff from '@mui/icons-material/VisibilityOff'
import {
  Box,
  Button,
  FormHelperText,
  IconButton,
  InputAdornment,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useRef, useState } from 'react'
import { FormGrid, FormGridItem } from '../ui'
import {
  parseServiceAccountFile,
  type FirebaseConfigFieldValues,
} from '../../utils/firebaseIdpConfig'

interface FirebaseProviderConfigFormProps {
  values: FirebaseConfigFieldValues
  onChange: (values: FirebaseConfigFieldValues) => void
  mode: 'create' | 'update'
  fileError?: string | null
  onFileError?: (message: string | null) => void
}

export function FirebaseProviderConfigForm({
  values,
  onChange,
  mode,
  fileError,
  onFileError,
}: FirebaseProviderConfigFormProps) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [showWebApiKey, setShowWebApiKey] = useState(false)

  async function handleServiceAccountFile(file: File | undefined): Promise<void> {
    if (!file) {
      return
    }

    const { data, error } = await parseServiceAccountFile(file)
    onFileError?.(error)
    if (error || !data) {
      onChange({
        ...values,
        serviceAccount: null,
        serviceAccountFileName: null,
      })
      return
    }

    const fileProjectId =
      typeof data.project_id === 'string' ? data.project_id.trim() : ''
    onChange({
      ...values,
      projectId: values.projectId.trim() || fileProjectId,
      serviceAccount: data,
      serviceAccountFileName: file.name,
    })
  }

  return (
    <Stack spacing={4} sx={{ width: '100%', pt: 0.5 }}>
      <Box>
        <Typography variant="subtitle2" sx={{ mb: 2 }}>
          Projeto
        </Typography>
        <FormGrid>
          <FormGridItem xs={12} md={6}>
            <TextField
              label="ID do projeto"
              value={values.projectId}
              onChange={(event) => onChange({ ...values, projectId: event.target.value })}
              required={mode === 'create'}
              fullWidth
              placeholder="meu-app-firebase"
              helperText="Configurações → Geral → ID do projeto"
            />
          </FormGridItem>
          <FormGridItem xs={12} md={6}>
            <TextField
              label="Web API Key"
              type={showWebApiKey ? 'text' : 'password'}
              value={values.webApiKey}
              onChange={(event) => onChange({ ...values, webApiKey: event.target.value })}
              required={mode === 'create'}
              fullWidth
              placeholder="AIzaSy..."
              helperText="Configurações → Geral → Chave da API da Web"
              slotProps={{
                input: {
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        aria-label={showWebApiKey ? 'Ocultar chave' : 'Mostrar chave'}
                        onClick={() => setShowWebApiKey((visible) => !visible)}
                        edge="end"
                      >
                        {showWebApiKey ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
                      </IconButton>
                    </InputAdornment>
                  ),
                },
              }}
            />
          </FormGridItem>
        </FormGrid>
      </Box>

      <Box>
        <Typography variant="subtitle2" sx={{ mb: 2 }}>
          Conta de serviço
        </Typography>
        <Stack spacing={1.5}>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap' }} useFlexGap>
            <Button
              variant="outlined"
              startIcon={<UploadFileOutlinedIcon />}
              onClick={() => fileInputRef.current?.click()}
            >
              {mode === 'update' ? 'Substituir arquivo JSON' : 'Enviar arquivo JSON'}
            </Button>
            {values.serviceAccountFileName ? (
              <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
                <CheckCircleOutlinedIcon color="success" fontSize="small" />
                <Typography variant="body2" color="text.secondary">
                  {values.serviceAccountFileName}
                </Typography>
              </Stack>
            ) : null}
          </Stack>
          <FormHelperText sx={{ mx: 0 }}>
            {mode === 'update'
              ? 'Opcional. Deixe em branco para manter a conta atual.'
              : 'Arquivo *-firebase-adminsdk-*.json (Contas de serviço → Admin SDK).'}
          </FormHelperText>
          {fileError ? (
            <FormHelperText error sx={{ mx: 0 }}>
              {fileError}
            </FormHelperText>
          ) : null}
          <input
            ref={fileInputRef}
            type="file"
            accept=".json,application/json"
            hidden
            onChange={(event) => {
              const file = event.target.files?.[0]
              void handleServiceAccountFile(file)
              event.target.value = ''
            }}
          />
        </Stack>
      </Box>
    </Stack>
  )
}

export const emptyFirebaseConfigFields = (): FirebaseConfigFieldValues => ({
  projectId: '',
  webApiKey: '',
  serviceAccount: null,
  serviceAccountFileName: null,
})
