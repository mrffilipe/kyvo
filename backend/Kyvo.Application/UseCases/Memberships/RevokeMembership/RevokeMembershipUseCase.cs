using Kyvo.Application.Policies;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Memberships.RevokeMembership;

public sealed class RevokeMembershipUseCase : IRevokeMembershipUseCase
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly ITenantAuthorizationPolicy _authorizationPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeMembershipUseCase(
        ITenantMembershipRepository memberships,
        ITenantAuthorizationPolicy authorizationPolicy,
        IUnitOfWork unitOfWork)
    {
        _memberships = memberships;
        _authorizationPolicy = authorizationPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(RevokeMembershipRequest request, CancellationToken ct = default)
    {
        var membership = await _memberships.GetForUpdateAsync(request.MembershipId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.TenantMembership.MEMBERSHIP_NOT_FOUND);

        await _authorizationPolicy.EnsureTenantAdministrativeAccessAsync(
            membership.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            ct);

        if (MembershipHasOwnerRole(membership))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantMembership.OWNER_MEMBERSHIP_CANNOT_BE_REVOKED);
        }

        membership.Revoke();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static bool MembershipHasOwnerRole(Domain.Entities.TenantMembership membership) =>
        membership.Roles.Any(role =>
            role.Role.Key.Value.Equals(TenantRoleDefaults.OWNER, StringComparison.OrdinalIgnoreCase));
}
