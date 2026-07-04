using Kyvo.Application.Common;

namespace Kyvo.Application.Queries.Memberships.ListMembershipsByTenant;

public sealed record ListMembershipsByTenantRequest : PagedRequest
{
    public Guid TenantId { get; init; }
    public Guid ActorUserId { get; init; }
    public required IReadOnlyCollection<string> ActorPlatformRoles { get; init; }
}
