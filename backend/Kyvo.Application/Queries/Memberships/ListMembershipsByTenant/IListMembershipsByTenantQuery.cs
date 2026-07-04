using Kyvo.Application.Common;
using Kyvo.Application.Queries.Memberships.Dtos;

namespace Kyvo.Application.Queries.Memberships.ListMembershipsByTenant;

public interface IListMembershipsByTenantQuery
{
    Task<PagedResult<MembershipDto>> ExecuteAsync(ListMembershipsByTenantRequest request, CancellationToken ct = default);
}
