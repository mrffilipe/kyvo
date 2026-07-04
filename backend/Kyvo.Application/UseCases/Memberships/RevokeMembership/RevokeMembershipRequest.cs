namespace Kyvo.Application.UseCases.Memberships.RevokeMembership;

public sealed record RevokeMembershipRequest
{
    public required Guid MembershipId { get; init; }
    public Guid ActorUserId { get; init; }
    public required IReadOnlyCollection<string> ActorPlatformRoles { get; init; }
}
