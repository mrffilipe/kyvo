/**
 * Referência do ConfigJson do IdP Firebase (montado pela UI a partir dos campos do formulário).
 * O backend usa projectId, webApiKey e serviceAccount; authDomain é derivado do projectId quando omitido.
 */

/** Exemplo do payload persistido — apenas referência para documentação. */
export const FIREBASE_IDP_CONFIG_EXAMPLE = `{
  "projectId": "meu-projeto-firebase",
  "webApiKey": "AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
  "serviceAccount": { "...": "conteúdo do arquivo Admin SDK" }
}`
