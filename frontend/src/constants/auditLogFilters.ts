/** Valores persistidos em audit_logs.action (enum AuditAction no backend). */
export const AUDIT_ACTION_OPTIONS = [
  { value: 'UserCreated', label: 'Usuário criado' },
  { value: 'UserUpdated', label: 'Usuário atualizado' },
  { value: 'TenantCreated', label: 'Tenant criado' },
  { value: 'TenantUpdated', label: 'Tenant atualizado' },
  { value: 'SessionCreated', label: 'Sessão criada' },
  { value: 'SessionRevoked', label: 'Sessão revogada' },
  { value: 'MembershipCreated', label: 'Membro adicionado' },
  { value: 'MembershipRevoked', label: 'Membro revogado' },
  { value: 'MembershipRoleUpdated', label: 'Papel do membro atualizado' },
  { value: 'InviteCreated', label: 'Convite criado' },
  { value: 'InviteAccepted', label: 'Convite aceito' },
] as const

/** Nome da entidade em audit_logs.resource_type. */
export const AUDIT_RESOURCE_TYPE_OPTIONS = [
  { value: 'User', label: 'Usuário' },
  { value: 'Tenant', label: 'Tenant' },
  { value: 'TenantMembership', label: 'Membership' },
  { value: 'TenantMembershipRole', label: 'Papel de membership' },
  { value: 'AuthSession', label: 'Sessão' },
  { value: 'TenantInvite', label: 'Convite' },
] as const
