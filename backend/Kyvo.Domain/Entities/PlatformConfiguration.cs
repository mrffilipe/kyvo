using Kyvo.Domain.Common;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.Entities;

public sealed class PlatformConfiguration : BaseEntity
{
    public bool IsBootstrapped { get; private set; }
    public Guid? RootUserId { get; private set; }
    public string? OauthClientId { get; private set; }
    public DateTime? BootstrappedAt { get; private set; }

    private PlatformConfiguration()
    {
    }

    public PlatformConfiguration(bool isBootstrapped = false)
    {
        IsBootstrapped = isBootstrapped;
    }

    public void MarkBootstrapped(Guid rootUserId, string oauthClientId)
    {
        if (IsBootstrapped)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.PlatformConfiguration.ALREADY_BOOTSTRAPPED);
        }

        if (rootUserId == Guid.Empty)
        {
            throw new DomainValidationException(DomainErrorMessages.PlatformConfiguration.ROOT_USER_ID_REQUIRED);
        }

        if (string.IsNullOrWhiteSpace(oauthClientId))
        {
            throw new DomainValidationException(DomainErrorMessages.PlatformConfiguration.OAUTH_CLIENT_ID_REQUIRED);
        }

        IsBootstrapped = true;
        RootUserId = rootUserId;
        OauthClientId = oauthClientId.Trim();
        BootstrappedAt = DateTime.UtcNow;
    }
}
