using Kyvo.Domain.Common;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Rules;

namespace Kyvo.Domain.Entities;

public sealed class TenantMembership : TenantEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public bool IsActive { get; private set; }
    public DateTime JoinedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

    public ICollection<TenantMembershipRole> Roles { get; private set; } = [];

    private TenantMembership()
    {
    }

    public TenantMembership(Guid tenantId, Guid userId, IEnumerable<TenantRole> roles) : base(tenantId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainValidationException(DomainErrorMessages.TenantMembership.USER_ID_REQUIRED);
        }

        UserId = userId;
        IsActive = true;
        JoinedAt = DateTime.UtcNow;
        ReplaceRoles(roles);
    }

    public void ReplaceRoles(IEnumerable<TenantRole> roles)
    {
        if (!IsActive)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.CANNOT_CHANGE_REVOKED_MEMBERSHIP_ROLES);
        }

        var normalizedRoles = TenantRoleAssignmentRules.ValidateForTenant(TenantId, roles);
        Roles.Clear();
        foreach (var role in normalizedRoles)
        {
            Roles.Add(new TenantMembershipRole(TenantId, Id, role));
        }
    }

    public void MergeRoles(IEnumerable<TenantRole> roles)
    {
        if (!IsActive)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.CANNOT_CHANGE_REVOKED_MEMBERSHIP_ROLES);
        }

        var existingRoleIds = Roles.Select(x => x.RoleId).ToHashSet();
        foreach (var role in TenantRoleAssignmentRules.ValidateForTenant(TenantId, roles))
        {
            if (existingRoleIds.Add(role.Id))
            {
                Roles.Add(new TenantMembershipRole(TenantId, Id, role));
            }
        }
    }

    public void Revoke() => IsActive = false;
}
