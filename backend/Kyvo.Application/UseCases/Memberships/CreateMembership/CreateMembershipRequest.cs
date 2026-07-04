namespace Kyvo.Application.UseCases.Memberships.CreateMembership;

public sealed record CreateMembershipRequest
{
    public required Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public Guid ActorUserId { get; init; }
    public required IReadOnlyCollection<string> ActorPlatformRoles { get; init; }
}
