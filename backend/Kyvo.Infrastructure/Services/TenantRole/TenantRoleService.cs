using Kyvo.Application.Common;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.TenantRoles;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Services.TenantRoles;

public sealed class TenantRoleService : ITenantRoleService
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantRoleRepository _roles;
    private readonly ITenantMembershipRepository _memberships;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public TenantRoleService(
        ITenantRepository tenants,
        ITenantRoleRepository roles,
        ITenantMembershipRepository memberships,
        IUnitOfWork unitOfWork,
        ApplicationDbContext context)
    {
        _tenants = tenants;
        _roles = roles;
        _memberships = memberships;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Guid> CreateTenantRoleAsync(CreateTenantRoleRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureActorCanManageTenantRolesAsync(
            request.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            cancellationToken);

        _ = await _tenants.GetForUpdateAsync(request.TenantId, cancellationToken)
            ?? throw new DomainNotFoundException(DomainErrorMessages.Tenant.TenantNotFound);

        var key = new TenantRoleKey(request.Key);
        if (await _roles.KeyAlreadyExistsAsync(request.TenantId, key, cancellationToken))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.RoleAlreadyExists);
        }

        var role = new Domain.Entities.TenantRole(request.TenantId, key, request.Name, request.Description);
        await _roles.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return role.Id;
    }

    public async Task UpdateTenantRoleAsync(UpdateTenantRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _roles.GetForUpdateAsync(request.RoleId, cancellationToken)
            ?? throw new DomainNotFoundException(DomainErrorMessages.TenantRole.RoleNotFound);

        await EnsureActorCanManageTenantRolesAsync(
            role.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            cancellationToken);

        if (role.IsSystem && !request.IsActive)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.SystemRoleCannotBeDeactivated);
        }

        role.UpdateDetails(request.Name, request.Description);
        if (request.IsActive)
        {
            role.Activate();
        }
        else
        {
            role.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTenantRoleAsync(DeleteTenantRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _roles.GetForUpdateAsync(request.RoleId, cancellationToken)
            ?? throw new DomainNotFoundException(DomainErrorMessages.TenantRole.RoleNotFound);

        await EnsureActorCanManageTenantRolesAsync(
            role.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            cancellationToken);

        if (role.IsSystem)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.SystemRoleCannotBeDeleted);
        }

        var hasActiveAssignments = await (
            from membershipRole in _context.TenantMembershipRoles.AsNoTracking()
            join membership in _context.TenantMemberships.AsNoTracking()
                on membershipRole.MembershipId equals membership.Id
            where membershipRole.RoleId == role.Id && membership.IsActive
            select membershipRole).AnyAsync(cancellationToken);

        if (hasActiveAssignments)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.RoleHasActiveAssignments);
        }

        _context.TenantRoles.Remove(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<TenantRoleDto>> ListTenantRolesAsync(
        ListTenantRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var roles = await _roles.ListByTenantIdAsync(
            request.TenantId,
            request.IncludeInactive,
            cancellationToken);

        var items = roles
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TenantRoleDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Key = x.Key.Value,
                Name = x.Name,
                Description = x.Description,
                IsSystem = x.IsSystem,
                IsActive = x.IsActive
            })
            .ToList();

        return new PagedResult<TenantRoleDto>
        {
            Items = items,
            Total = roles.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task EnsureActorCanManageTenantRolesAsync(
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
}
