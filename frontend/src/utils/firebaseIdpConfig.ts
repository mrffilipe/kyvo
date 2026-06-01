import type { FirebaseProviderConfig } from '../types'

export const BOOTSTRAP_LOCAL_IDP_ALIAS = 'local'

export function isBootstrapLocalProvider(providerType: string, alias: string): boolean {
  return providerType === 'Local' || alias.trim().toLowerCase() === BOOTSTRAP_LOCAL_IDP_ALIAS
}

export interface FirebaseConfigFieldValues {
  projectId: string
  webApiKey: string
  serviceAccount: Record<string, unknown> | null
  serviceAccountFileName: string | null
}

export function buildFirebaseConfigJson(values: FirebaseConfigFieldValues): string {
  if (!values.serviceAccount) {
    throw new Error('serviceAccount is required to build Firebase config JSON.')
  }
  return JSON.stringify({
    projectId: values.projectId.trim(),
    webApiKey: values.webApiKey.trim(),
    serviceAccount: values.serviceAccount,
  } satisfies FirebaseProviderConfig)
}

export function validateFirebaseConfigFields(
  values: FirebaseConfigFieldValues,
  mode: 'create' | 'update',
): string | null {
  const projectId = values.projectId.trim()
  const webApiKey = values.webApiKey.trim()
  const hasServiceAccount = values.serviceAccount !== null

  if (mode === 'create') {
    if (!projectId) {
      return 'Informe o ID do projeto Firebase.'
    }
    if (!webApiKey) {
      return 'Informe a chave de API Web (Web API Key).'
    }
    if (!hasServiceAccount) {
      return 'Envie o arquivo JSON da conta de serviço (Firebase Admin SDK).'
    }
  } else {
    const anyFilled = Boolean(projectId || webApiKey || hasServiceAccount)
    if (!anyFilled) {
      return null
    }
    if (!projectId || !webApiKey || !hasServiceAccount) {
      return 'Para alterar a configuração, preencha o ID do projeto, a Web API Key e envie o arquivo da conta de serviço.'
    }
  }

  if (!hasServiceAccount || values.serviceAccount === null) {
    return null
  }

  const accountType = values.serviceAccount.type
  if (accountType !== 'service_account') {
    return 'O arquivo enviado não é uma conta de serviço Firebase (type deve ser "service_account").'
  }

  const fileProjectId = values.serviceAccount.project_id
  if (typeof fileProjectId === 'string' && fileProjectId.trim() && fileProjectId.trim() !== projectId) {
    return 'O project_id do arquivo da conta de serviço deve coincidir com o ID do projeto informado.'
  }

  return null
}

export async function parseServiceAccountFile(file: File): Promise<{
  data: Record<string, unknown> | null
  error: string | null
}> {
  if (!file.name.toLowerCase().endsWith('.json')) {
    return { data: null, error: 'Selecione um arquivo .json baixado do Firebase Console.' }
  }

  try {
    const text = await file.text()
    const parsed = JSON.parse(text) as unknown
    if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
      return { data: null, error: 'O arquivo JSON da conta de serviço é inválido.' }
    }
    return { data: parsed as Record<string, unknown>, error: null }
  } catch {
    return { data: null, error: 'Não foi possível ler o arquivo JSON da conta de serviço.' }
  }
}
