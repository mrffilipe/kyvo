using Kyvo.Application.Common;
using Kyvo.Application.Queries.Users.Dtos;

namespace Kyvo.Application.Queries.Users.SearchUsers;

public interface ISearchUsersQuery
{
    Task<PagedResult<UserPickerDto>> ExecuteAsync(SearchUsersRequest request, CancellationToken ct = default);
}
