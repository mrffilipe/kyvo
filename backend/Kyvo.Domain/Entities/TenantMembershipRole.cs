using Kyvo.Domain.Common;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.Entities;

public sealed class TenantMembershipRole : TenantEntity
{
    public Guid MembershipId { get; private set; }
    public TenantMembership Membership { get; private set; } = null!;

    public Guid RoleId { get; private set; }
    public TenantRole Role { get; private set; } = null!;

    private TenantMembershipRole()
    {
    }

    public TenantMembershipRole(Guid tenantId, Guid membershipId, TenantRole role) : base(tenantId)
    {
        if (membershipId == Guid.Empty)
        {
            throw new DomainValidationException(DomainErrorMessages.TenantMembership.MEMBERSHIP_NOT_FOUND);
        }

        if (role.TenantId != tenantId)
        {
            throw new DomainValidationException(DomainErrorMessages.TenantRole.ROLE_TENANT_MISMATCH);
        }

        MembershipId = membershipId;
        RoleId = role.Id;
        Role = role;
    }
}
