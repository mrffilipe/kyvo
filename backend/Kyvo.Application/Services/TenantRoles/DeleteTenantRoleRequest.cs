namespace Kyvo.Application.Services.TenantRoles;

public sealed record DeleteTenantRoleRequest
{
    public Guid RoleId { get; init; }

    public Guid ActorUserId { get; init; }

    public IReadOnlyCollection<string> ActorPlatformRoles { get; init; } = [];
}
