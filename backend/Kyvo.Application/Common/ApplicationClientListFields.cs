using System.Text.Json;
using Kyvo.Application.Exceptions;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Application.Common;

public static class ApplicationClientListFields
{
    public static IReadOnlyList<string> ParseRedirectUris(string? json) => ParseJsonArray(json);
    public static IReadOnlyList<string> ParseAllowedScopes(string? json) => ParseJsonArray(json);

    public static string ToRedirectUrisJson(string raw)
    {
        var values = Parse(raw);
        if (values.Count == 0)
        {
            throw new DomainValidationException(ApplicationErrorMessages.OAuthClient.RedirectUrisRequired);
        }

        ValidateRedirectUris(values);
        return JsonSerializer.Serialize(values);
    }

    public static string ToAllowedScopesJson(string? raw, IReadOnlyList<string>? scopesList)
    {
        var values = scopesList is { Count: > 0 }
            ? Normalize(scopesList)
            : Parse(raw ?? string.Empty);

        if (values.Count == 0)
        {
            throw new DomainValidationException(ApplicationErrorMessages.OAuthClient.AllowedScopesRequired);
        }

        ValidateAllowedScopes(values);
        return JsonSerializer.Serialize(values);
    }

    private static void ValidateRedirectUris(IReadOnlyList<string> values)
    {
        foreach (var value in values)
        {
            if (!IsValidAbsoluteHttpUrl(value))
            {
                throw new DomainValidationException(ApplicationErrorMessages.OAuthClient.RedirectUriInvalidFormat);
            }
        }
    }

    private static void ValidateAllowedScopes(IReadOnlyList<string> values)
    {
        foreach (var value in values)
        {
            if (!OAuthScopeDefaults.AllowedValues.Contains(value))
            {
                throw new DomainValidationException(
                    string.Format(ApplicationErrorMessages.OAuthClient.AllowedScopeNotPermitted, value));
            }
        }
    }

    private static bool IsValidAbsoluteHttpUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static IReadOnlyList<string> Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith('['))
        {
            try
            {
                var jsonValues = JsonSerializer.Deserialize<string[]>(trimmed) ?? [];
                return Normalize(jsonValues);
            }
            catch (JsonException)
            {
                throw new DomainValidationException(ApplicationErrorMessages.OAuthClient.ConfigurationInvalid);
            }
        }

        if (trimmed.Contains(',') || trimmed.Contains('\n') || trimmed.Contains('\r') || trimmed.Contains(';'))
        {
            return Normalize(trimmed.Split([',', '\n', '\r', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return Normalize(trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static IReadOnlyList<string> Normalize(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
