using Kyvo.Domain.Common;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.ValueObjects;

namespace Kyvo.Domain.Entities;

public sealed class ExternalIdentity : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string Provider { get; private set; } = default!;
    public string ProviderUserId { get; private set; } = default!;
    public EmailAddress Email { get; private set; } = null!;

    private ExternalIdentity()
    {
    }

    public ExternalIdentity(
        Guid userId,
        string provider,
        string providerUserId,
        string email)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainValidationException(DomainErrorMessages.ExternalIdentity.UserIdRequired);
        }

        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerUserId))
        {
            throw new DomainValidationException(DomainErrorMessages.ExternalIdentity.ProviderDataRequired);
        }

        UserId = userId;
        Provider = provider.Trim().ToLowerInvariant();
        ProviderUserId = providerUserId.Trim();
        Email = new EmailAddress(email);
    }
}
