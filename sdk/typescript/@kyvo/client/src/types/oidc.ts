/** OIDC DTOs — aligned with Kyvo OpenAPI (`/swagger/oidc/swagger.json`). */

export interface OidcUserInfoResponse {
  sub?: string
  email?: string
  name?: string
  tid?: string
  mid?: string
  trole?: string[]
  prole?: string
}
