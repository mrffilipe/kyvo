using Kyvo.Application.Common;

namespace Kyvo.Application.Queries.Users.SearchUsers;

public sealed record SearchUsersRequest : PagedRequest
{
    public required string Search { get; init; }
}
