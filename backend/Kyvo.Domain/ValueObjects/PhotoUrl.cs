using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.ValueObjects;

public sealed record PhotoUrl
{
    public const int MAX_LENGTH = 500;

    public string Value { get; private set; } = default!;

    private PhotoUrl()
    {
    }

    public PhotoUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainValidationException(DomainErrorMessages.PhotoUrl.REQUIRED);
        }

        var normalized = url.Trim();
        if (normalized.Length > MAX_LENGTH)
        {
            throw new DomainValidationException(DomainErrorMessages.PhotoUrl.MAX_LENGTH);
        }

        if (!IsValidAbsoluteHttpUrl(normalized))
        {
            throw new DomainValidationException(DomainErrorMessages.PhotoUrl.INVALID_FORMAT);
        }

        Value = normalized;
    }

    public static PhotoUrl? FromNullable(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return new PhotoUrl(url);
    }

    public static implicit operator string?(PhotoUrl? value) => value?.Value;

    public static implicit operator PhotoUrl?(string? value) => FromNullable(value);

    private static bool IsValidAbsoluteHttpUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
