using Kyvo.Application.Common;
using Kyvo.Application.Queries.Users.ListUserMemberships;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Kyvo.Application.Queries.Users.Dtos;

namespace Kyvo.Infrastructure.Queries.Users;

public sealed class ListUserMembershipsQuery : IListUserMembershipsQuery
{
    private readonly ApplicationDbContext _context;

    public ListUserMembershipsQuery(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<UserMembershipDto>> ExecuteAsync(ListUserMembershipsRequest request, CancellationToken ct = default)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var query = _context.TenantMemberships
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .Where(x => x.UserId == request.UserId && x.IsActive);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.Tenant.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserMembershipDto
            {
                MembershipId = x.Id,
                TenantId = x.TenantId,
                TenantName = x.Tenant.Name,
                TenantKey = x.Tenant.Key.Value,
                Roles = x.Roles.Select(role => role.Role.Key.Value).ToList()
            })
            .ToListAsync(ct);

        return new PagedResult<UserMembershipDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
