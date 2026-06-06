namespace Kyvo.Application.Services.Membership;

public sealed record CreateMembershipRequest
{
    public required Guid UserId { get; init; }

    public Guid TenantId { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }

    public Guid ActorUserId { get; init; }

    public IReadOnlyCollection<string> ActorPlatformRoles { get; init; } = [];
}
