using Kyvo.Application.Policies;
using Kyvo.Application.Services.TenantRoles;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Memberships.UpdateMembershipRoles;

public sealed class UpdateMembershipRolesUseCase : IUpdateMembershipRolesUseCase
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly ITenantRoleResolver _roleResolver;
    private readonly ITenantAuthorizationPolicy _authorizationPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMembershipRolesUseCase(
        ITenantMembershipRepository memberships,
        ITenantRoleResolver roleResolver,
        ITenantAuthorizationPolicy authorizationPolicy,
        IUnitOfWork unitOfWork)
    {
        _memberships = memberships;
        _roleResolver = roleResolver;
        _authorizationPolicy = authorizationPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(UpdateMembershipRolesRequest request, CancellationToken ct = default)
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
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantMembership.OWNER_ROLE_CANNOT_BE_CHANGED);
        }

        var roles = await _roleResolver.ResolveActiveRolesAsync(
            membership.TenantId,
            request.Roles,
            ct);

        membership.ReplaceRoles(roles);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static bool MembershipHasOwnerRole(Domain.Entities.TenantMembership membership) =>
        membership.Roles.Any(role =>
            role.Role.Key.Value.Equals(TenantRoleDefaults.OWNER, StringComparison.OrdinalIgnoreCase));
}
