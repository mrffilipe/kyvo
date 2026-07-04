using Kyvo.Domain.Common;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Rules;
using Kyvo.Domain.ValueObjects;

namespace Kyvo.Domain.Entities;

public sealed class TenantInvite : TenantEntity
{
    public EmailAddress Email { get; private set; } = null!;
    public string TokenHash { get; private set; } = default!;
    public string? EncryptedToken { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public Guid InvitedByUserId { get; private set; }
    public User InvitedByUser { get; private set; } = null!;

    public ICollection<TenantInviteRole> Roles { get; private set; } = [];

    private TenantInvite()
    {
    }

    public TenantInvite(
        Guid tenantId,
        string email,
        IEnumerable<TenantRole> roles,
        string tokenHash,
        string encryptedToken,
        DateTime expiresAt,
        Guid invitedByUserId) : base(tenantId)
    {
        if (invitedByUserId == Guid.Empty)
        {
            throw new DomainValidationException(DomainErrorMessages.TenantInvite.INVITED_BY_USER_ID_REQUIRED);
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainValidationException(DomainErrorMessages.TenantInvite.TOKEN_HASH_REQUIRED);
        }

        if (string.IsNullOrWhiteSpace(encryptedToken))
        {
            throw new DomainValidationException(DomainErrorMessages.TenantInvite.ENCRYPTED_TOKEN_REQUIRED);
        }

        Email = new EmailAddress(email);
        TokenHash = tokenHash.Trim();
        EncryptedToken = encryptedToken.Trim();
        ExpiresAt = expiresAt;
        InvitedByUserId = invitedByUserId;
        ReplaceRoles(roles);
    }

    public bool IsExpired() => ExpiresAt <= DateTime.UtcNow;

    public bool IsConsumed() => ConsumedAt.HasValue;

    public bool IsRevoked() => RevokedAt.HasValue;

    public TenantInviteStatus GetStatus()
    {
        if (IsConsumed())
        {
            return TenantInviteStatus.Accepted;
        }

        if (IsRevoked())
        {
            return TenantInviteStatus.Revoked;
        }

        if (IsExpired())
        {
            return TenantInviteStatus.Expired;
        }

        return TenantInviteStatus.Pending;
    }

    public void Consume()
    {
        if (IsConsumed())
        {
            return;
        }

        ConsumedAt = DateTime.UtcNow;
    }

    public void Revoke()
    {
        if (IsConsumed())
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantInvite.CANNOT_REVOKE_CONSUMED);
        }

        if (IsRevoked())
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantInvite.ALREADY_REVOKED);
        }

        RevokedAt = DateTime.UtcNow;
    }

    public void ReplaceRoles(IEnumerable<TenantRole> roles)
    {
        var normalizedRoles = TenantRoleAssignmentRules.ValidateForTenant(TenantId, roles);
        Roles.Clear();
        foreach (var role in normalizedRoles)
        {
            Roles.Add(new TenantInviteRole(TenantId, Id, role));
        }
    }
}
