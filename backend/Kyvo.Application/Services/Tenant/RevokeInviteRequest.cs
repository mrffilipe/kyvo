namespace Kyvo.Application.Services.Tenant;

public sealed record RevokeInviteRequest
{
    public Guid InviteId { get; init; }
    public Guid ActorUserId { get; init; }
    public IReadOnlyList<string> ActorPlatformRoles { get; init; } = [];
}
