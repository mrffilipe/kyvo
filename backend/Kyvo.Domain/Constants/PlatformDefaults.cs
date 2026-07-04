namespace Kyvo.Domain.Constants;

/// <summary>
/// Fixed constants for the platform admin console application.
/// These values are not configurable via API or panel; they are part of the product contract.
/// </summary>
public static class PlatformDefaults
{
    public static class AdminConsole
    {
        public const string APPLICATION_NAME = "Platform Admin";
        public const string APPLICATION_SLUG = "platform-admin";
        public const string CLIENT_ID = "platform-admin-web";

        public static readonly IReadOnlyList<string> AllowedScopes =
            ["openid", "profile", "email", "offline_access"];

        public static readonly IReadOnlyList<string> DefaultRedirectUris =
            ["http://localhost:3000/auth/callback"];

        public static readonly IReadOnlyList<string> DefaultPostLogoutRedirectUris =
            ["http://localhost:3000/login"];
    }

    public static class LocalIdentityProvider
    {
        public const string ALIAS = "local";
        public const string DISPLAY_NAME = "Local";
    }
}
