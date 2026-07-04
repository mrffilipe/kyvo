using Kyvo.Domain.Common;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.Entities;

public sealed class TenantInviteRole : TenantEntity
{
    public Guid InviteId { get; private set; }
    public TenantInvite Invite { get; private set; } = null!;

    public Guid RoleId { get; private set; }
    public TenantRole Role { get; private set; } = null!;

    private TenantInviteRole()
    {
    }

    public TenantInviteRole(Guid tenantId, Guid inviteId, TenantRole role) : base(tenantId)
    {
        if (inviteId == Guid.Empty)
        {
            throw new DomainValidationException(DomainErrorMessages.TenantInvite.INVITE_NOT_FOUND);
        }

        if (role.TenantId != tenantId)
        {
            throw new DomainValidationException(DomainErrorMessages.TenantRole.ROLE_TENANT_MISMATCH);
        }

        InviteId = inviteId;
        RoleId = role.Id;
        Role = role;
    }
}
