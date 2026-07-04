using Kyvo.Domain.Common;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.Entities;

public sealed class PlatformRole : BaseEntity
{
    public string Key { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsSystem { get; private set; }

    public ICollection<UserPlatformRole> UserAssignments { get; private set; } = [];

    private PlatformRole()
    {
    }

    public PlatformRole(string key, string name, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainValidationException(DomainErrorMessages.PlatformRole.KEY_REQUIRED);
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(key.Trim(), @"^[a-z0-9_]+$"))
        {
            throw new DomainValidationException(DomainErrorMessages.PlatformRole.KEY_INVALID_FORMAT);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException(DomainErrorMessages.PlatformRole.NAME_REQUIRED);
        }

        if (name.Trim().Length > 120)
        {
            throw new DomainValidationException(DomainErrorMessages.PlatformRole.NAME_MAX_LENGTH);
        }

        Key = key.Trim().ToLowerInvariant();
        Name = name.Trim();
        IsSystem = isSystem;
    }
}
