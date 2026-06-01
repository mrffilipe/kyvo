using System.Text.RegularExpressions;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.Common;

public static partial class BrandingColor
{
    private static readonly Regex HexPattern = HexColorRegex();

    public static string NormalizeOrThrow(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(
                string.Format(DomainErrorMessages.Application.BrandingColorRequired, fieldName));
        }

        var trimmed = value.Trim();
        if (!HexPattern.IsMatch(trimmed))
        {
            throw new DomainValidationException(
                string.Format(DomainErrorMessages.Application.BrandingColorInvalid, fieldName));
        }

        return trimmed.ToLowerInvariant();
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorRegex();
}
