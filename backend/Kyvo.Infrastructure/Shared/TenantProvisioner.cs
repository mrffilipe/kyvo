using Kyvo.Application.Shared;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;

namespace Kyvo.Infrastructure.Shared;

public sealed class TenantProvisioner : ITenantProvisioner
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantRoleRepository _roles;
    private readonly ITenantMembershipRepository _memberships;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public TenantProvisioner(
        ITenantRepository tenants,
        ITenantRoleRepository roles,
        ITenantMembershipRepository memberships,
        IUserRepository users,
        IUnitOfWork unitOfWork)
    {
        _tenants = tenants;
        _roles = roles;
        _memberships = memberships;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<TenantProvisionResult> ProvisionAsync(TenantProvisionRequest request, CancellationToken ct = default)
    {
        var tenantKey = new TenantKey(request.TenantKey);
        if (await _tenants.KeyAlreadyExistsAsync(tenantKey, ct))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.Tenant.KEY_ALREADY_EXISTS);
        }

        var owner = await _users.GetForUpdateAsync(request.OwnerUserId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.User.USER_NOT_FOUND);

        if (!owner.IsActive)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.User.USER_INACTIVE);
        }

        var tenant = new Domain.Entities.Tenant(request.TenantName, tenantKey);
        await _tenants.AddAsync(tenant, ct);

        Domain.Entities.TenantRole? ownerRole = null;
        foreach (var role in TenantRoleDefaults.All)
        {
            var createdRole = new Domain.Entities.TenantRole(
                tenant.Id,
                role.Key,
                role.Name,
                isSystem: true);
            await _roles.AddAsync(createdRole, ct);

            if (role.Key.Equals(TenantRoleDefaults.OWNER, StringComparison.OrdinalIgnoreCase))
            {
                ownerRole = createdRole;
            }
        }

        if (ownerRole is null)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.AT_LEAST_ONE_ROLE_REQUIRED);
        }

        var membership = new TenantMembership(tenant.Id, request.OwnerUserId, [ownerRole]);
        await _memberships.AddAsync(membership, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new TenantProvisionResult
        {
            TenantId = tenant.Id,
            MembershipId = membership.Id,
            OwnerRoleId = ownerRole.Id,
            TenantKey = tenantKey.Value
        };
    }
}
