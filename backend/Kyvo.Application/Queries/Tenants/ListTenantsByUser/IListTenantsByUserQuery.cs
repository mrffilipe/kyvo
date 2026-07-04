using Kyvo.Application.Common;
using Kyvo.Application.Queries.Tenants.Dtos;

namespace Kyvo.Application.Queries.Tenants.ListTenantsByUser;

public interface IListTenantsByUserQuery
{
    Task<PagedResult<TenantDto>> ExecuteAsync(ListTenantsByUserRequest request, CancellationToken ct = default);
}
