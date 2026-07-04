using System.Text.RegularExpressions;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.ValueObjects;

public sealed record TenantRoleKey
{
    private static readonly Regex KeyRegex = new("^[a-z0-9][a-z0-9-]{1,62}$", RegexOptions.Compiled);

    public string Value { get; private set; } = default!;

    private TenantRoleKey()
    {
    }

    public TenantRoleKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainValidationException(DomainErrorMessages.TenantRole.KEY_REQUIRED);
        }

        var normalized = key.Trim().ToLowerInvariant();
        if (!KeyRegex.IsMatch(normalized))
        {
            throw new DomainValidationException(DomainErrorMessages.TenantRole.KEY_INVALID_FORMAT);
        }

        Value = normalized;
    }

    public static implicit operator string(TenantRoleKey value) => value.Value;
    public static implicit operator TenantRoleKey(string value) => new(value);
}
