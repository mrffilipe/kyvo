using Kyvo.Application.Common;

namespace Kyvo.Application.Queries.Tenants.ListTenantsByUser;

public sealed record ListTenantsByUserRequest : PagedRequest
{
    public Guid UserId { get; init; }
    public string? Search { get; init; }
    public required IReadOnlyList<string> ActorPlatformRoles { get; init; }
}
