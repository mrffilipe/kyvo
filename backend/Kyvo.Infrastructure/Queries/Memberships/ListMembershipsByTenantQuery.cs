using Kyvo.Application.Common;
using Kyvo.Application.Policies;
using Kyvo.Application.Queries.Memberships.ListMembershipsByTenant;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Kyvo.Application.Queries.Memberships.Dtos;

namespace Kyvo.Infrastructure.Queries.Memberships;

public sealed class ListMembershipsByTenantQuery : IListMembershipsByTenantQuery
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantAuthorizationPolicy _authorizationPolicy;

    public ListMembershipsByTenantQuery(
        ApplicationDbContext context,
        ITenantAuthorizationPolicy authorizationPolicy)
    {
        _context = context;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<PagedResult<MembershipDto>> ExecuteAsync(ListMembershipsByTenantRequest request, CancellationToken ct = default)
    {
        await _authorizationPolicy.EnsureTenantAdministrativeAccessAsync(
            request.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            ct);

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var query = _context.TenantMemberships
            .AsNoTracking()
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .Where(x => x.TenantId == request.TenantId);

        var total = await query.CountAsync(ct);
        var memberships = await query
            .OrderBy(x => x.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var userIds = memberships.Select(x => x.UserId).Distinct().ToList();
        var users = await _context.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var items = memberships
            .Select(membership =>
            {
                users.TryGetValue(membership.UserId, out var user);
                return new MembershipDto
                {
                    Id = membership.Id,
                    UserId = membership.UserId,
                    UserEmail = user?.Email,
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
}
