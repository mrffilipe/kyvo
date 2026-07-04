namespace Kyvo.Application.UseCases.Invites.RevokeInvite;

public sealed record RevokeInviteRequest
{
    public required Guid InviteId { get; init; }
    public required Guid ActorUserId { get; init; }
    public required IReadOnlyList<string> ActorPlatformRoles { get; init; }
}
