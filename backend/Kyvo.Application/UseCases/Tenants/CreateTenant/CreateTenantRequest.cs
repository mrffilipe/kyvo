namespace Kyvo.Application.UseCases.Tenants.CreateTenant;

public sealed record CreateTenantRequest
{
    public required string Name { get; init; }
    public required string Key { get; init; }
    public Guid ActorUserId { get; init; }
    public Guid? InitialAdministratorUserId { get; init; }
    public string? InitialAdministratorEmail { get; init; }
    public required IReadOnlyList<string> ActorPlatformRoles { get; init; }
}
