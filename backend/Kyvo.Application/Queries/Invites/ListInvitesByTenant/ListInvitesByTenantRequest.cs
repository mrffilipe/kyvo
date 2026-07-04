using Kyvo.Application.Common;

namespace Kyvo.Application.Queries.Invites.ListInvitesByTenant;

public sealed record ListInvitesByTenantRequest : PagedRequest
{
    public required Guid TenantId { get; init; }
    public required Guid ActorUserId { get; init; }
    public required IReadOnlyList<string> ActorPlatformRoles { get; init; }
}
