namespace Kyvo.Application.Exceptions;

public static class ApplicationErrorMessages
{
    public static class OAuthClient
    {
        public const string CLIENT_ID_REQUIRED = "client_id is required.";
        public const string CLIENT_ID_INVALID = "client_id is invalid.";
        public const string CLIENT_SECRET_REQUIRED = "client_secret is required for confidential clients.";
        public const string CLIENT_SECRET_NOT_ALLOWED_FOR_PUBLIC_CLIENTS = "Public clients must not provide a client secret.";
        public const string CLIENT_SECRET_INVALID = "client_secret is invalid.";
        public const string REDIRECT_URI_NOT_ALLOWED = "redirect_uri is not allowed for this client.";
        public const string REDIRECT_URIS_REQUIRED = "At least one redirect URI is required.";
        public const string REDIRECT_URI_INVALID_FORMAT = "Each redirect URI must be a valid absolute http or https URL.";
        public const string ALLOWED_SCOPES_REQUIRED = "At least one allowed scope is required.";
        public const string ALLOWED_SCOPE_NOT_PERMITTED = "Scope is not permitted: {0}.";
        public const string CONFIGURATION_INVALID = "Client configuration is invalid.";
        public const string REQUESTED_SCOPES_NOT_ALLOWED = "Requested scopes are not allowed: {0}.";
        public const string PLATFORM_ADMIN_CONSOLE_ACCESS_DENIED = "Access denied. Only platform administrators may use the Platform Admin console.";
        public const string POST_LOGOUT_REDIRECT_URI_NOT_ALLOWED = "post_logout_redirect_uri is not allowed for this client.";
        public const string CLIENT_ID_REQUIRED_FOR_LOGOUT_REDIRECT = "client_id is required when post_logout_redirect_uri is specified.";
    }

    public static class OAuthAuthorization
    {
        public const string RESPONSE_TYPE_MUST_BE_CODE = "response_type must be code.";
        public const string UNSUPPORTED_GRANT_TYPE = "The specified grant type is not supported.";
        public const string INTERACTIVE_LOGIN_REQUIRED = "Interactive login is required.";
        public const string MISSING_LOGIN_CONTEXT = "Login context is missing from the authentication cookie.";
        public const string SESSION_NO_LONGER_ACTIVE = "Session is no longer active.";
        public const string UNABLE_TO_BUILD_CLAIMS = "Unable to build token claims.";
    }

    public static class Pkce
    {
        public const string CODE_CHALLENGE_REQUIRED = "Public clients must send code_challenge.";
        public const string CODE_CHALLENGE_LENGTH = "code_challenge must be between 43 and 128 characters.";
        public const string CODE_CHALLENGE_METHOD_UNSUPPORTED = "Unsupported code_challenge_method.";
    }

    public static class Auth
    {
        public const string USER_HAS_NO_CLIENT_TENANT_MEMBERSHIP = "User has no active membership for this client tenant.";
        public const string PLATFORM_BOOTSTRAP_REQUIRED = "Platform bootstrap is required before login.";
        public const string PLATFORM_BOOTSTRAP_ALREADY_COMPLETED = "Platform bootstrap has already been completed.";
        public const string PLATFORM_BOOTSTRAP_ADMIN_CREDENTIALS_NOT_CONFIGURED = "Bootstrap admin credentials are not configured. Set Bootstrap__AdminEmail and Bootstrap__AdminPassword environment variables (or Bootstrap:AdminEmail / Bootstrap:AdminPassword in appsettings).";
        public const string PLATFORM_BOOTSTRAP_APPLICATION_SLUG_ALREADY_EXISTS = "Bootstrap application slug already exists.";
        public const string PLATFORM_BOOTSTRAP_CLIENT_ID_ALREADY_EXISTS = "Bootstrap OAuth client id already exists.";
        public const string SESSION_INACTIVE = "Session is not active.";
        public const string AUTHENTICATED_SESSION_REQUIRED = "Authenticated session is required.";
        public const string SESSION_HAS_NO_OAUTH_CLIENT = "Current session has no OAuth client context.";
        public const string USER_HAS_NO_TENANT_ACCESS = "User has no access to this tenant.";
        public const string SESSION_NOT_FOUND = "Session not found.";
        public const string USER_NOT_FOUND = "User not found.";
        public const string CANNOT_REVOKE_ANOTHER_USER_SESSION = "You do not have permission to revoke this session.";
        public const string LOCAL_AUTH_INVALID_CREDENTIALS = "Invalid email or password.";
        public const string ACCOUNT_DELETION_BLOCKED = "Account cannot be deleted while there are pending issues for this application tenant.";
        public const string APPLICATION_TENANT_NOT_FOUND = "Application tenant link not found for the current session.";
        public const string ACTIVE_TENANT_CONTEXT_REQUIRED = "An active tenant context is required to delete the account for this application.";
    }

    public static class Search
    {
        public const string QUERY_TOO_SHORT = "Search query must be at least 3 characters.";
    }

    public static class Application
    {
        public const string NOT_FOUND = "Application not found.";
        public const string SLUG_ALREADY_EXISTS = "Application slug already exists.";
        public const string SYSTEM_APPLICATION_CANNOT_BE_MODIFIED = "System applications cannot be modified.";
        public const string BRANDING_LOGO_FILE_REQUIRED = "A logo file is required.";
        public const string BRANDING_LOGO_FILE_TOO_LARGE = "Logo file must be 512 KB or smaller.";
        public const string BRANDING_LOGO_FILE_TYPE_NOT_ALLOWED = "Logo must be PNG, JPEG, WebP, or SVG.";
    }

    public static class IdentityProvider
    {
        public const string NOT_FOUND = "Identity provider not found.";
        public const string ALIAS_ALREADY_EXISTS = "Identity provider alias already exists.";
        public const string CANNOT_DISABLE_LAST_LOCAL_PROVIDER = "Cannot disable the only active local identity provider.";
        public const string DISABLED = "Identity provider is disabled.";
        public const string LOCAL_NOT_ALLOWED_FOR_EXTERNAL_LOGIN = "Local identity provider cannot be used for external login.";
        public const string CONFIG_INVALID = "Identity provider configuration is invalid.";
        public const string CONFIG_REQUIRED = "Identity provider configuration (ConfigJson) is required for this provider type.";
        public const string LOGIN_TYPE_NOT_SUPPORTED = "Login is not yet supported for this identity provider type.";
        public const string LOCAL_PROVIDER_CREATION_NOT_ALLOWED = "The local email/password identity provider is created during platform bootstrap and cannot be added again.";
        public const string LOCAL_PROVIDER_MODIFICATION_NOT_ALLOWED = "The local email/password identity provider cannot be modified.";
        public const string LOCAL_PROVIDER_DISABLE_NOT_ALLOWED = "The local email/password identity provider cannot be disabled.";
    }

    public static class Signing
    {
        public const string RSA_KEY_NOT_AVAILABLE = "RSA key is not available.";
    }

    public static class Registration
    {
        public const string EMAIL_REQUIRED = "Email is required.";
        public const string EMAIL_ALREADY_EXISTS = "An account with this email already exists.";
        public const string PASSWORD_REQUIRED = "Password is required.";
        public const string PASSWORD_TOO_WEAK = "Password does not meet the minimum policy.";
        public const string DISPLAY_NAME_REQUIRED = "Display name is required.";
        public const string LOCAL_PASSWORD_DISABLED = "Self-registration is disabled because no local identity provider is enabled.";
    }

    public static class IdentityProviderCapability
    {
        public const string LOCAL_PASSWORD_ALREADY_HANDLED = "Another active identity provider already handles email/password authentication.";
        public const string SOCIAL_ALREADY_HANDLED_FORMAT = "Another enabled provider already advertises the {0} capability: {1}.";
    }
}
