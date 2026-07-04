namespace Kyvo.Infrastructure.Common;

public static class InfrastructureErrorMessages
{
    public static class Bootstrap
    {
        public const string ADMIN_EMAIL_REQUIRED_WHEN_ANY_PROVIDED = "Bootstrap:AdminEmail is required when any bootstrap option is provided.";
        public const string ADMIN_EMAIL_INVALID = "Bootstrap:AdminEmail must be a valid email address.";
        public const string ADMIN_PASSWORD_REQUIRED_WHEN_ANY_PROVIDED = "Bootstrap:AdminPassword is required when any bootstrap option is provided.";
    }

    public static class Database
    {
        public const string CONNECTION_STRING_REQUIRED = "Database:ConnectionString is required.";
    }

    public static class Email
    {
        public const string FROM_ADDRESS_REQUIRED = "Email:FromAddress is required.";
        public const string FROM_ADDRESS_INVALID = "Email:FromAddress must be a valid email address.";
        public const string REGION_REQUIRED = "Email:Region is required.";
    }

    public static class Invite
    {
        public const string EXPIRATION_HOURS_REQUIRED = "Invite:ExpirationHours is required.";
        public const string EXPIRATION_HOURS_MUST_BE_POSITIVE = "Invite:ExpirationHours must be greater than zero.";
    }

    public static class Jwt
    {
        public const string ISSUER_REQUIRED = "Jwt:Issuer is required.";
        public const string ISSUER_MUST_BE_ABSOLUTE_URI = "Jwt:Issuer must be an absolute URI (e.g. http://localhost:5000).";
        public const string AUDIENCE_REQUIRED = "Jwt:Audience is required.";
        public const string REFRESH_TOKEN_DAYS_REQUIRED = "Jwt:RefreshTokenDays is required.";
        public const string REFRESH_TOKEN_DAYS_MUST_BE_POSITIVE = "Jwt:RefreshTokenDays must be greater than zero.";
        public const string SIGNING_KEY_SOURCE_REQUIRED = "Jwt:SigningKeyPath, Jwt:SigningKeyPem, or Jwt:SigningKeyPemBase64 is required.";
        public const string SIGNING_KEY_SOURCE_MUST_BE_EXCLUSIVE = "Configure only one of Jwt:SigningKeyPath, Jwt:SigningKeyPem, or Jwt:SigningKeyPemBase64.";
        public const string KEY_ID_REQUIRED = "Jwt:KeyId is required.";
    }

    public static class PasswordPolicy
    {
        public const string MIN_LENGTH_REQUIRED = "PasswordPolicy:MinLength is required.";
        public const string MIN_LENGTH_MUST_BE_AT_LEAST_EIGHT = "PasswordPolicy:MinLength must be at least 8.";
        public const string MUST_CONTAIN_LETTER = "Passwords must contain at least one letter.";
    }

    public static class RateLimit
    {
        public const string ACCOUNT_REGISTER_PERMIT_LIMIT_REQUIRED = "RateLimit:AccountRegisterPermitLimit is required.";
        public const string ACCOUNT_REGISTER_PERMIT_LIMIT_MUST_BE_POSITIVE = "RateLimit:AccountRegisterPermitLimit must be greater than zero.";
        public const string ACCOUNT_REGISTER_WINDOW_MINUTES_REQUIRED = "RateLimit:AccountRegisterWindowMinutes is required.";
        public const string ACCOUNT_REGISTER_WINDOW_MINUTES_MUST_BE_POSITIVE = "RateLimit:AccountRegisterWindowMinutes must be greater than zero.";
    }

    public static class Redis
    {
        public const string INSTANCE_NAME_REQUIRED = "Redis:InstanceName is required.";
        public const string INSTANCE_NAME_REQUIRED_WHEN_CONNECTION_STRING_SET = "Redis:InstanceName is required when Redis:ConnectionString is set.";
        public const string TENANT_IDENTIFIER_CACHE_MINUTES_REQUIRED = "Redis:TenantIdentifierCacheMinutes is required.";
        public const string TENANT_IDENTIFIER_CACHE_MINUTES_MUST_BE_POSITIVE = "Redis:TenantIdentifierCacheMinutes must be greater than zero.";
    }

    public static class SecretProtection
    {
        public const string KEY_DIRECTORY_PATH_REQUIRED = "SecretProtection:KeyDirectoryPath is required.";
        public const string APPLICATION_NAME_REQUIRED = "SecretProtection:ApplicationName is required.";
    }
}
