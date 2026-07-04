namespace Kyvo.Application.UseCases.Memberships.UpdateMembershipRoles;

public sealed record UpdateMembershipRolesRequest
{
    public Guid MembershipId { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }
    public Guid ActorUserId { get; init; }
    public required IReadOnlyCollection<string> ActorPlatformRoles { get; init; }
}
