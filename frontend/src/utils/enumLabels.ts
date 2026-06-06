import { ApplicationType, ClientType, type ApplicationType as ApplicationTypeValue, type ClientType as ClientTypeValue } from '../types/applications'
import { SessionStatus, type SessionStatus as SessionStatusValue } from '../types/auth'

const applicationTypeLabels: Record<ApplicationTypeValue, string> = {
  [ApplicationType.Web]: 'Web',
  [ApplicationType.Mobile]: 'Mobile',
  [ApplicationType.Backend]: 'Backend',
}

const clientTypeLabels: Record<ClientTypeValue, string> = {
  [ClientType.Public]: 'Público',
  [ClientType.Confidential]: 'Confidencial',
}

const tenantInviteStatusLabels: Record<string, string> = {
  Pending: 'Pendente',
  Accepted: 'Aceito',
  Expired: 'Expirado',
  Revoked: 'Revogado',
}

const sessionStatusLabels: Record<SessionStatusValue, string> = {
  [SessionStatus.Active]: 'Ativa',
  [SessionStatus.Revoked]: 'Revogada',
  [SessionStatus.Expired]: 'Expirada',
}

export function applicationTypeLabel(type: ApplicationTypeValue | string | number | undefined): string {
  if (type === undefined || type === null || type === '') {
    return '—'
  }
  if (typeof type === 'number') {
    const legacy: ApplicationTypeValue[] = [ApplicationType.Web, ApplicationType.Mobile, ApplicationType.Backend]
    const mapped = legacy[type]
    return mapped ? applicationTypeLabels[mapped] : String(type)
  }
  const key = String(type) as ApplicationTypeValue
  return applicationTypeLabels[key] ?? key
}

export function clientTypeLabel(type: ClientTypeValue | string | number | undefined): string {
  if (type === undefined || type === null || type === '') {
    return '—'
  }
  if (typeof type === 'number') {
    const legacy: ClientTypeValue[] = [ClientType.Public, ClientType.Confidential]
    const mapped = legacy[type]
    return mapped ? clientTypeLabels[mapped] : String(type)
  }
  const key = String(type) as ClientTypeValue
  return clientTypeLabels[key] ?? key
}

export function sessionStatusLabel(status: SessionStatusValue | string | number | undefined): string {
  if (status === undefined || status === null || status === '') {
    return '—'
  }
  if (typeof status === 'number') {
    const legacy: SessionStatusValue[] = [SessionStatus.Active, SessionStatus.Revoked, SessionStatus.Expired]
    const mapped = legacy[status]
    return mapped ? sessionStatusLabels[mapped] : String(status)
  }
  const key = String(status) as SessionStatusValue
  return sessionStatusLabels[key] ?? key
}

const tenantRoleLabels: Record<string, string> = {
  owner: 'Proprietário',
  admin: 'Administrador',
  member: 'Membro',
  viewer: 'Visualizador',
}

const platformRoleLabels: Record<string, string> = {
  plat_admin: 'Administrador da plataforma',
}

export function tenantRoleLabel(key: string | undefined | null): string {
  if (!key) {
    return '—'
  }
  const normalized = key.trim().toLowerCase()
  return tenantRoleLabels[normalized] ?? key
}

export function platformRoleLabel(key: string | undefined | null): string {
  if (!key) {
    return '—'
  }
  const normalized = key.trim().toLowerCase()
  return platformRoleLabels[normalized] ?? key
}

export function tenantRoleDisplayName(role: { key: string; name: string; isSystem?: boolean }): string {
  if (role.isSystem) {
    return tenantRoleLabel(role.key)
  }
  return role.name?.trim() ? role.name : tenantRoleLabel(role.key)
}

export function tenantInviteStatusLabel(status: string | undefined): string {
  if (!status) {
    return '—'
  }
  return tenantInviteStatusLabels[status] ?? status
}
