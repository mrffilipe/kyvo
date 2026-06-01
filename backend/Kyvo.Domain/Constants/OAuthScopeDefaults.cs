namespace Kyvo.Domain.Constants;

public static class OAuthScopeDefaults
{
    public static readonly IReadOnlySet<string> AllowedValues =
        new HashSet<string>(
        [
            "openid",
            "profile",
            "email",
            "offline_access"
        ],
        StringComparer.OrdinalIgnoreCase);
}
