const IDP_ALIAS_REGEX = /^[a-z0-9_-]+$/

export function isValidIdpAlias(value: string): boolean {
  const normalized = value.trim().toLowerCase()
  return normalized.length >= 1 && IDP_ALIAS_REGEX.test(normalized)
}

export function normalizeIdpAliasInput(value: string): string {
  return value.trim().toLowerCase()
}
