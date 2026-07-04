namespace Kyvo.Domain.Exceptions;

public static class DomainErrorMessages
{
    public static class TenantEntity
    {
        public const string TENANT_ID_REQUIRED = "TenantId is required.";
    }

    public static class Tenant
    {
        public const string NAME_REQUIRED = "Tenant name is required.";
        public const string KEY_ALREADY_EXISTS = "Tenant key already exists.";
        public const string TENANT_NOT_FOUND = "Tenant not found.";
    }

    public static class TenantKey
    {
        public const string REQUIRED = "Tenant key is required.";
        public const string INVALID_FORMAT = "Tenant key format is invalid.";
    }

    public static class TenantRole
    {
        public const string KEY_REQUIRED = "Tenant role key is required.";
        public const string KEY_INVALID_FORMAT = "Tenant role key format is invalid.";
        public const string NAME_REQUIRED = "Tenant role name is required.";
        public const string NAME_MAX_LENGTH = "Tenant role name max length is 120.";
        public const string DESCRIPTION_MAX_LENGTH = "Tenant role description max length is 500.";
        public const string AT_LEAST_ONE_ROLE_REQUIRED = "At least one tenant role is required.";
        public const string ROLE_TENANT_MISMATCH = "Tenant role does not belong to this tenant.";
        public const string INACTIVE_ROLE = "Tenant role is inactive.";
        public const string DUPLICATE_ROLE = "Tenant roles cannot contain duplicates.";
        public const string ROLE_NOT_FOUND = "Tenant role not found.";
        public const string ROLE_ALREADY_EXISTS = "Tenant role key already exists.";
        public const string CANNOT_CHANGE_REVOKED_MEMBERSHIP_ROLES = "Cannot change roles for a revoked membership.";
        public const string SYSTEM_ROLE_CANNOT_BE_DEACTIVATED = "System tenant roles cannot be deactivated.";
        public const string SYSTEM_ROLE_CANNOT_BE_DELETED = "System tenant roles cannot be deleted.";
        public const string ROLE_HAS_ACTIVE_ASSIGNMENTS = "Tenant role is assigned to active memberships and cannot be deleted.";
    }

    public static class User
    {
        public const string DISPLAY_NAME_REQUIRED = "Display name is required.";
        public const string USER_NOT_FOUND = "User not found.";
        public const string USER_INACTIVE = "User is inactive.";
        public const string EMAIL_ALREADY_EXISTS = "User email already exists.";
    }

    public static class EmailAddress
    {
        public const string REQUIRED = "Email is required.";
        public const string MAX_LENGTH = "Email max length is 255.";
        public const string INVALID_FORMAT = "Email format is invalid.";
    }

    public static class PhotoUrl
    {
        public const string REQUIRED = "Photo URL is required.";
        public const string MAX_LENGTH = "Photo URL max length is 500.";
        public const string INVALID_FORMAT = "Photo URL must be a valid absolute http or https URL.";
    }

    public static class TenantMembership
    {
        public const string USER_ID_REQUIRED = "UserId is required.";
        public const string MEMBERSHIP_ALREADY_EXISTS = "Membership already exists.";
        public const string MEMBERSHIP_NOT_FOUND = "Membership not found.";
        public const string OWNER_ROLE_CANNOT_BE_CHANGED = "The tenant owner role cannot be changed.";
        public const string OWNER_MEMBERSHIP_CANNOT_BE_REVOKED = "The tenant owner membership cannot be revoked.";
    }

    public static class TenantInvite
    {
        public const string INVITED_BY_USER_ID_REQUIRED = "InvitedByUserId is required.";
        public const string TOKEN_HASH_REQUIRED = "Invite token hash is required.";
        public const string INVITE_NOT_FOUND = "Invite not found.";
        public const string ALREADY_CONSUMED = "Invite was already consumed.";
        public const string EXPIRED = "Invite has expired.";
        public const string EMAIL_MISMATCH = "Invite email does not match authenticated user.";
        public const string ENCRYPTED_TOKEN_REQUIRED = "Invite encrypted token is required.";
        public const string ALREADY_REVOKED = "Invite was already revoked.";
        public const string CANNOT_REVOKE_CONSUMED = "Cannot revoke an invite that was already accepted.";
        public const string REVOKED = "Invite was revoked.";
    }

    public static class Application
    {
        public const string NAME_AND_SLUG_REQUIRED = "Application name and slug are required.";
        public const string BRANDING_SYSTEM_APPLICATION_READ_ONLY = "System applications cannot customize login branding.";
        public const string BRANDING_PRIMARY_COLOR_FIELD = "Primary color";
        public const string BRANDING_SECONDARY_COLOR_FIELD = "Secondary color";
        public const string BRANDING_COLORS_REQUIRED_WHEN_ENABLED = "Primary and secondary colors are required when branding is enabled.";
        public const string BRANDING_COLOR_REQUIRED = "{0} is required.";
        public const string BRANDING_COLOR_INVALID = "{0} must be a hex color (#RRGGBB).";
        public const string BRANDING_LOGO_PATH_INVALID = "Branding logo path is invalid.";
        public const string BRANDING_HERO_TITLE_TOO_LONG = "Hero title must be at most {0} characters.";
        public const string BRANDING_HERO_SUBTITLE_TOO_LONG = "Hero subtitle must be at most {0} characters.";
    }

    public static class ApplicationClient
    {
        public const string DATA_INVALID = "Application client data is invalid.";
    }

    public static class ApplicationTenant
    {
        public const string DATA_INVALID = "Application tenant data is invalid.";
        public const string MAPPING_ALREADY_EXISTS = "Application tenant mapping already exists.";
    }

    public static class AuthSession
    {
        public const string USER_ID_REQUIRED = "UserId is required.";
        public const string CLIENT_ID_REQUIRED = "OAuth client id is required.";
        public const string TENANT_CONTEXT_INVALID = "Tenant context is invalid.";
        public const string SESSION_NOT_FOUND = "Session not found.";
    }

    public static class PlatformConfiguration
    {
        public const string NOT_FOUND = "Platform configuration not found.";
        public const string ALREADY_BOOTSTRAPPED = "Platform bootstrap has already been completed.";
        public const string ROOT_USER_ID_REQUIRED = "Root user id is required.";
        public const string OAUTH_CLIENT_ID_REQUIRED = "OAuth client id is required.";
    }

    public static class PlatformRole
    {
        public const string KEY_REQUIRED = "Platform role key is required.";
        public const string KEY_INVALID_FORMAT = "Platform role key format is invalid.";
        public const string NAME_REQUIRED = "Platform role name is required.";
        public const string NAME_MAX_LENGTH = "Platform role name max length is 120.";
        public const string ROLE_NOT_FOUND = "Platform role not found.";
    }

    public static class UserPlatformRole
    {
        public const string USER_ID_REQUIRED = "UserId is required.";
        public const string ROLE_ID_REQUIRED = "RoleId is required.";
    }

    public static class IdentityProvider
    {
        public const string ALIAS_REQUIRED = "Identity provider alias is required.";
        public const string ALIAS_INVALID_FORMAT = "Identity provider alias format is invalid.";
        public const string DISPLAY_NAME_REQUIRED = "Identity provider display name is required.";
        public const string ALIAS_ALREADY_EXISTS = "Identity provider alias already exists.";
        public const string NOT_FOUND = "Identity provider not found.";
        public const string CANNOT_DISABLE_LAST_LOCAL_PROVIDER = "Cannot disable the only active local identity provider.";
        public const string LOCAL_PASSWORD_RESERVED_FOR_LOCAL = "The LocalPassword capability is only allowed for the Local provider type.";
        public const string CAPABILITIES_REQUIRED = "At least one capability is required for non-Local providers.";
    }
}
