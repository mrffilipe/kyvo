namespace Kyvo.Application.UseCases.TenantRoles.UpdateTenantRole;

public sealed record UpdateTenantRoleRequest
{
    public required Guid RoleId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public required Guid ActorUserId { get; init; }
    public required IReadOnlyCollection<string> ActorPlatformRoles { get; init; }
}
