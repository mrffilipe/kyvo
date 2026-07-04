export const OAUTH_SCOPE_OPTIONS = ['openid', 'profile', 'email', 'offline_access'] as const

export type OAuthScope = (typeof OAUTH_SCOPE_OPTIONS)[number]
