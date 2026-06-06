namespace Kyvo.Application.Services.Membership;

public sealed record RevokeMembershipRequest
{
    public required Guid MembershipId { get; init; }

    public Guid ActorUserId { get; init; }

    public IReadOnlyCollection<string> ActorPlatformRoles { get; init; } = [];
}
