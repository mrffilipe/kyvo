using Kyvo.Application.Common;

namespace Kyvo.Application.Queries.Users.ListUserMemberships;

public sealed record ListUserMembershipsRequest : PagedRequest
{
    public Guid UserId { get; init; }
}
