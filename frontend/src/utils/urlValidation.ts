export function isAbsoluteHttpUrl(value: string | null | undefined): boolean {
  if (!value?.trim()) {
    return false
  }

  try {
    const url = new URL(value.trim())
    return url.protocol === 'http:' || url.protocol === 'https:'
  } catch {
    return false
  }
}

export function isValidRedirectUri(value: string): boolean {
  return isAbsoluteHttpUrl(value)
}
