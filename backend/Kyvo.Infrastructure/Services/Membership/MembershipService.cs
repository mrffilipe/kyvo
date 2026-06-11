using Kyvo.Application.Common;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.Membership;
using Kyvo.Application.Services.TenantRoles;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Services.Membership;

public sealed class MembershipService : IMembershipService
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly ITenantRoleResolver _roleResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public MembershipService(
        ITenantMembershipRepository memberships,
        ITenantRoleResolver roleResolver,
        IUnitOfWork unitOfWork,
        ApplicationDbContext context)
    {
        _memberships = memberships;
        _roleResolver = roleResolver;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Guid> CreateMembershipAsync(CreateMembershipRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureActorCanManageMembershipsAsync(
            request.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            cancellationToken);

        var existing = await _memberships.GetByUserIdAndTenantIdWithRolesAsync(
            request.UserId,
            request.TenantId,
            cancellationToken);

        if (existing is not null && existing.IsActive)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantMembership.MembershipAlreadyExists);
        }

        var roles = await _roleResolver.ResolveActiveRolesAsync(
            request.TenantId,
            request.Roles,
            cancellationToken);

        var membership = new TenantMembership(request.TenantId, request.UserId, roles);
        await _memberships.AddAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return membership.Id;
    }

    public async Task UpdateRolesAsync(UpdateMembershipRolesRequest request, CancellationToken cancellationToken = default)
    {
        var membership = await _memberships.GetForUpdateAsync(request.MembershipId, cancellationToken)
            ?? throw new DomainNotFoundException(DomainErrorMessages.TenantMembership.MembershipNotFound);

        await EnsureActorCanManageMembershipsAsync(
            membership.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            cancellationToken);

        if (MembershipHasOwnerRole(membership))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantMembership.OwnerRoleCannotBeChanged);
        }

        var roles = await _roleResolver.ResolveActiveRolesAsync(
            membership.TenantId,
            request.Roles,
            cancellationToken);

        membership.ReplaceRoles(roles);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeMembershipAsync(RevokeMembershipRequest request, CancellationToken cancellationToken = default)
    {
        var membership = await _memberships.GetForUpdateAsync(request.MembershipId, cancellationToken)
            ?? throw new DomainNotFoundException(DomainErrorMessages.TenantMembership.MembershipNotFound);

        await EnsureActorCanManageMembershipsAsync(
            membership.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            cancellationToken);

        if (MembershipHasOwnerRole(membership))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantMembership.OwnerMembershipCannotBeRevoked);
        }

        membership.Revoke();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<MembershipDto>> ListByTenantAsync(
        ListMembershipsByTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureActorCanManageMembershipsAsync(
            request.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            cancellationToken);

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var query = _context.TenantMemberships
            .AsNoTracking()
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .Where(x => x.TenantId == request.TenantId);

        var total = await query.CountAsync(cancellationToken);
        var memberships = await query
            .OrderBy(x => x.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = memberships.Select(x => x.UserId).Distinct().ToList();
        var users = await _context.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var items = memberships
            .Select(membership =>
            {
                users.TryGetValue(membership.UserId, out var user);
                return new MembershipDto
                {
                    Id = membership.Id,
                    UserId = membership.UserId,
                    UserEmail = user?.Email.Value,
                    UserDisplayName = user?.DisplayName,
                    TenantId = membership.TenantId,
                    Roles = membership.Roles.Select(role => role.Role.Key.Value).ToList(),
                    IsActive = membership.IsActive
                };
            })
            .ToList();

        return new PagedResult<MembershipDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task EnsureActorCanManageMembershipsAsync(
        Guid tenantId,
        Guid actorUserId,
        IReadOnlyCollection<string> actorPlatformRoles,
        CancellationToken cancellationToken)
    {
        if (actorPlatformRoles.Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role)))
        {
            return;
        }

        var membership = await _memberships.GetByUserIdAndTenantIdWithRolesAsync(
            actorUserId,
            tenantId,
            cancellationToken);

        var hasAdministrativeRole = membership is not null
            && membership.IsActive
            && membership.Roles.Any(role => TenantRoleDefaults.AdministrativeKeys.Contains(role.Role.Key.Value));

        if (!hasAdministrativeRole)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.UserHasNoTenantAccess);
        }
    }

    private static bool MembershipHasOwnerRole(TenantMembership membership) =>
        membership.Roles.Any(role =>
            role.Role.Key.Value.Equals(TenantRoleDefaults.Owner, StringComparison.OrdinalIgnoreCase));
}
