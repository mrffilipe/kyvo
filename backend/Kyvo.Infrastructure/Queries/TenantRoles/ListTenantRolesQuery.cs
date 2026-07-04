using Kyvo.Application.Common;
using Kyvo.Application.Queries.TenantRoles.Dtos;
using Kyvo.Application.Queries.TenantRoles.ListTenantRoles;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Queries.TenantRoles;

public sealed class ListTenantRolesQuery : IListTenantRolesQuery
{
    private readonly ITenantRoleRepository _roles;

    public ListTenantRolesQuery(ITenantRoleRepository roles) => _roles = roles;

    public async Task<PagedResult<TenantRoleDto>> ExecuteAsync(ListTenantRolesRequest request, CancellationToken ct = default)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var roles = await _roles.ListByTenantIdAsync(
            request.TenantId,
            request.IncludeInactive,
            ct);

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
}
