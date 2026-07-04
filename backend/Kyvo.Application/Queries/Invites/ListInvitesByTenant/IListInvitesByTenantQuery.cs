using Kyvo.Application.Common;
using Kyvo.Application.Queries.Invites.Dtos;

namespace Kyvo.Application.Queries.Invites.ListInvitesByTenant;

public interface IListInvitesByTenantQuery
{
    Task<PagedResult<TenantInviteDto>> ExecuteAsync(ListInvitesByTenantRequest request, CancellationToken ct = default);
}
