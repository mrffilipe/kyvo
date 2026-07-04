using Kyvo.Application.Common;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Queries.Tenants.ListTenantsByUser;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Exceptions;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Kyvo.Application.Queries.Tenants.Dtos;

namespace Kyvo.Infrastructure.Queries.Tenants;

public sealed class ListTenantsByUserQuery : IListTenantsByUserQuery
{
    private readonly ApplicationDbContext _context;

    public ListTenantsByUserQuery(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<TenantDto>> ExecuteAsync(ListTenantsByUserRequest request, CancellationToken ct = default)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var isPlatformAdministrator = request.ActorPlatformRoles
            .Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role));

        IQueryable<Domain.Entities.Tenant> tenantQuery;

        if (isPlatformAdministrator)
        {
            tenantQuery = _context.Tenants.AsNoTracking();
        }
        else
        {
            tenantQuery = _context.TenantMemberships
                .AsNoTracking()
                .Where(x => x.UserId == request.UserId && x.IsActive)
                .Select(x => x.Tenant);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            if (search.Length < 3)
            {
                throw new DomainValidationException(ApplicationErrorMessages.Search.QUERY_TOO_SHORT);
            }

            var pattern = $"%{search}%";
            tenantQuery = tenantQuery.Where(x =>
                EF.Functions.ILike(x.Name, pattern)
                || EF.Functions.ILike(x.Key.Value, pattern));
        }

        var total = await tenantQuery.CountAsync(ct);
        var items = await tenantQuery
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TenantDto
            {
                Id = x.Id,
                Name = x.Name,
                Key = x.Key.Value
            })
            .ToListAsync(ct);

        return new PagedResult<TenantDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
