using Kyvo.Domain.Constants;

namespace Kyvo.Application.Common;

public static class AdminConsoleClientDefaults
{
    public static IReadOnlyList<string> BuildRedirectUris(string issuer)
    {
        var uris = new HashSet<string>(StringComparer.Ordinal);
        foreach (var uri in PlatformDefaults.AdminConsole.DefaultRedirectUris)
        {
            uris.Add(uri);
        }

        var normalizedIssuer = issuer.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(normalizedIssuer))
        {
            uris.Add($"{normalizedIssuer}/auth/callback");
        }

        return uris.ToList();
    }

    public static IReadOnlyList<string> BuildPostLogoutRedirectUris(string issuer)
    {
        var uris = new HashSet<string>(StringComparer.Ordinal);
        foreach (var uri in PlatformDefaults.AdminConsole.DefaultPostLogoutRedirectUris)
        {
            uris.Add(uri);
        }

        var normalizedIssuer = issuer.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(normalizedIssuer))
        {
            uris.Add($"{normalizedIssuer}/login");
        }

        return uris.ToList();
    }
}
