using Kyvo.Application.Common;

namespace Kyvo.Application.Services.Users;

public sealed record SearchUsersRequest : PagedRequest
{
    public required string Search { get; init; }
}
