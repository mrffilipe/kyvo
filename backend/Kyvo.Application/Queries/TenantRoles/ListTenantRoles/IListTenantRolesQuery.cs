using Kyvo.Application.Common;
using Kyvo.Application.Queries.TenantRoles.Dtos;

namespace Kyvo.Application.Queries.TenantRoles.ListTenantRoles;

public interface IListTenantRolesQuery
{
    Task<PagedResult<TenantRoleDto>> ExecuteAsync(ListTenantRolesRequest request, CancellationToken ct = default);
}
