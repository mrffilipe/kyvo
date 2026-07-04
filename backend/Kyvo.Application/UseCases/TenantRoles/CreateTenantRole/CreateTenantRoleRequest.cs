namespace Kyvo.Application.UseCases.TenantRoles.CreateTenantRole;

public sealed record CreateTenantRoleRequest
{
    public required Guid TenantId { get; init; }
    public required string Key { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required Guid ActorUserId { get; init; }
    public required IReadOnlyCollection<string> ActorPlatformRoles { get; init; }
}
