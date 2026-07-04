namespace Kyvo.API.Common;

public static class ApiErrorMessages
{
    public const string UNAUTHORIZED_TITLE = "Unauthorized";
    public const string FORBIDDEN_TITLE = "Forbidden";
    public const string DOMAIN_VALIDATION_TITLE = "Domain Validation Error";
    public const string INVALID_CLIENT_TITLE = "Invalid Client";
    public const string DOMAIN_BUSINESS_RULE_TITLE = "Domain Business Rule Error";
    public const string NOT_FOUND_TITLE = "Not Found";
    public const string UNHANDLED_SERVER_ERROR_TITLE = "Unhandled Server Error";
    public const string UNEXPECTED_ERROR_DETAIL = "Unexpected error while processing the request.";
    public const string PROBLEM_JSON_CONTENT_TYPE = "application/problem+json";

    public static class OidcLogin
    {
        public const string MISSING_LOGIN_CONTEXT = "Login context is missing from the authentication cookie.";
        public const string INVALID_LOGIN_CONTEXT = "Login context is invalid.";
        public const string INTERACTIVE_LOGIN_REQUIRED = "Interactive login is required.";
        public const string SESSION_NO_LONGER_ACTIVE = "Session is no longer active.";
        public const string UNABLE_TO_BUILD_CLAIMS = "Unable to build token claims.";
        public const string PLATFORM_ADMIN_CONSOLE_ACCESS_DENIED = "Acesso negado. Apenas administradores da plataforma podem usar o console Platform Admin.";
    }

    public static class Account
    {
        public const string EMAIL_AND_PASSWORD_REQUIRED = "Informe o e-mail e a senha.";
        public const string INVALID_EMAIL_OR_PASSWORD = "E-mail ou senha inválidos.";
        public const string INVALID_PROVIDER_OR_TOKEN = "Provedor ou token inválido.";
        public const string SESSION_EXPIRED_RETRY_LOGIN = "Sua sessão expirou. Atualize a página e entre novamente.";
    }
}
