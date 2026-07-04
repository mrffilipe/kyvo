namespace Kyvo.Application.UseCases.TenantRoles.DeleteTenantRole;

public sealed record DeleteTenantRoleRequest
{
    public required Guid RoleId { get; init; }
    public required Guid ActorUserId { get; init; }
    public required IReadOnlyCollection<string> ActorPlatformRoles { get; init; } 
}
