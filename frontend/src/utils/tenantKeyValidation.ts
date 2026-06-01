const TENANT_KEY_REGEX = /^[a-z0-9][a-z0-9-]{1,62}$/

export function isValidTenantKey(value: string): boolean {
  return TENANT_KEY_REGEX.test(value.trim().toLowerCase())
}

export function normalizeTenantKeyInput(value: string): string {
  return value.trim().toLowerCase()
}
