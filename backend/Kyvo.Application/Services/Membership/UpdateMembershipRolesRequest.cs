namespace Kyvo.Application.Services.Membership;

public sealed record UpdateMembershipRolesRequest
{
    public Guid MembershipId { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }

    public Guid ActorUserId { get; init; }

    public IReadOnlyCollection<string> ActorPlatformRoles { get; init; } = [];
}
