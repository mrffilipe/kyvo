using Kyvo.Application.Policies;
using Kyvo.Application.Services.TenantRoles;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Memberships.CreateMembership;

public sealed class CreateMembershipUseCase : ICreateMembershipUseCase
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly ITenantRoleResolver _roleResolver;
    private readonly ITenantAuthorizationPolicy _authorizationPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMembershipUseCase(
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

    public async Task<Guid> ExecuteAsync(CreateMembershipRequest request, CancellationToken ct = default)
    {
        await _authorizationPolicy.EnsureTenantAdministrativeAccessAsync(
            request.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            ct);

        var existing = await _memberships.GetByUserIdAndTenantIdWithRolesAsync(
            request.UserId,
            request.TenantId,
            ct);

        if (existing is not null && existing.IsActive)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantMembership.MEMBERSHIP_ALREADY_EXISTS);
        }

        var roles = await _roleResolver.ResolveActiveRolesAsync(
            request.TenantId,
            request.Roles,
            ct);

        var membership = new TenantMembership(request.TenantId, request.UserId, roles);
        await _memberships.AddAsync(membership, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return membership.Id;
    }
}
