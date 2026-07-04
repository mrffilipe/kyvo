export function buildInviteUrl(acceptPath: string): string {
  if (typeof window === 'undefined') {
    return acceptPath
  }

  return `${window.location.origin}${acceptPath}`
}

export async function copyInviteLink(acceptPath: string): Promise<void> {
  await navigator.clipboard.writeText(buildInviteUrl(acceptPath))
}
