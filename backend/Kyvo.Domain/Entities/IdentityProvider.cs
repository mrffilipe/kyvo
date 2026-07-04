using Kyvo.Domain.Common;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.Entities;

public sealed class IdentityProvider : BaseEntity
{
    public string Alias { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public IdentityProviderType ProviderType { get; private set; }
    public bool Enabled { get; private set; }

    /// <summary>
    /// JSON-serialized <c>FederatedProviderConfig</c> (ClientId/ClientSecret/Issuer). <c>ClientSecret</c> is
    /// encrypted at rest by <c>IdentityProviderConfigCipher</c> using ASP.NET Core Data Protection.
    /// </summary>
    public string? ConfigJson { get; private set; }

    /// <summary>
    /// Authentication capabilities advertised by this provider on the login page.
    /// Hard-locked invariant: only <see cref="IdentityProviderType.Local"/> may advertise
    /// <see cref="IdpCapability.LocalPassword"/>, and a Local provider must advertise it.
    /// </summary>
    public ICollection<IdpCapability> Capabilities { get; private set; } = [];

    private IdentityProvider()
    {
    }

    public IdentityProvider(
        string alias,
        string displayName,
        IdentityProviderType providerType,
        IEnumerable<IdpCapability> capabilities,
        bool enabled = true,
        string? configJson = null)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            throw new DomainValidationException(DomainErrorMessages.IdentityProvider.ALIAS_REQUIRED);
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(alias.Trim(), @"^[a-z0-9_-]+$"))
        {
            throw new DomainValidationException(DomainErrorMessages.IdentityProvider.ALIAS_INVALID_FORMAT);
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainValidationException(DomainErrorMessages.IdentityProvider.DISPLAY_NAME_REQUIRED);
        }

        Alias = alias.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        ProviderType = providerType;
        Enabled = enabled;
        ConfigJson = configJson;
        Capabilities = NormalizeCapabilities(providerType, capabilities);
    }

    public void Enable() => Enabled = true;

    public void Disable() => Enabled = false;

    public void UpdateConfig(string? configJson) => ConfigJson = configJson;

    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainValidationException(DomainErrorMessages.IdentityProvider.DISPLAY_NAME_REQUIRED);
        }

        DisplayName = displayName.Trim();
    }

    public void UpdateCapabilities(IEnumerable<IdpCapability> capabilities)
    {
        Capabilities = NormalizeCapabilities(ProviderType, capabilities);
    }

    private static List<IdpCapability> NormalizeCapabilities(IdentityProviderType providerType, IEnumerable<IdpCapability> capabilities)
    {
        var distinct = capabilities?.Distinct().ToList() ?? new List<IdpCapability>();

        if (providerType == IdentityProviderType.Local)
        {
            if (distinct.Any(c => c != IdpCapability.LocalPassword))
            {
                throw new DomainValidationException(DomainErrorMessages.IdentityProvider.LOCAL_PASSWORD_RESERVED_FOR_LOCAL);
            }

            if (!distinct.Contains(IdpCapability.LocalPassword))
            {
                distinct.Add(IdpCapability.LocalPassword);
            }
        }
        else
        {
            if (distinct.Contains(IdpCapability.LocalPassword))
            {
                throw new DomainValidationException(DomainErrorMessages.IdentityProvider.LOCAL_PASSWORD_RESERVED_FOR_LOCAL);
            }

            if (distinct.Count == 0)
            {
                throw new DomainValidationException(DomainErrorMessages.IdentityProvider.CAPABILITIES_REQUIRED);
            }
        }

        return distinct;
    }
}
