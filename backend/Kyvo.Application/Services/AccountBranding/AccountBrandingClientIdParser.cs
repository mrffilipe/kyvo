namespace Kyvo.Application.Services.AccountBranding;

public static class AccountBrandingClientIdParser
{
    public const string ClientIdParameter = "client_id";

    public static string? ExtractClientId(string? returnUrl, string? clientIdQuery)
    {
        if (!string.IsNullOrWhiteSpace(clientIdQuery))
        {
            return clientIdQuery.Trim();
        }

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        var pathAndQuery = returnUrl.Trim();
        var queryIndex = pathAndQuery.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0)
        {
            return null;
        }

        var query = pathAndQuery[(queryIndex + 1)..];
        foreach (var segment in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = segment.Split('=', 2);
            if (parts.Length == 2 &&
                string.Equals(parts[0], ClientIdParameter, StringComparison.OrdinalIgnoreCase))
            {
                var value = Uri.UnescapeDataString(parts[1].Replace('+', ' '));
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
        }

        return null;
    }
}
