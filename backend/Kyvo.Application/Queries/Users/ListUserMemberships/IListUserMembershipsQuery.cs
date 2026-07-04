using Kyvo.Application.Common;
using Kyvo.Application.Queries.Users.Dtos;

namespace Kyvo.Application.Queries.Users.ListUserMemberships;

public interface IListUserMembershipsQuery
{
    Task<PagedResult<UserMembershipDto>> ExecuteAsync(ListUserMembershipsRequest request, CancellationToken ct = default);
}
