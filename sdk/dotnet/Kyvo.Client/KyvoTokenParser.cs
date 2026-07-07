using System.Text;
using System.Text.Json;

namespace Kyvo.Client;

internal static class KyvoTokenParser
{
    public static string? TryGetTokenUse(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return null;
        }

        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("token_use", out var claim)
                ? claim.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }
}
