namespace Kyvo.Application.Services.Tenant;

public sealed record CreateTenantRequest
{
    public required string Name { get; init; }

    public required string Key { get; init; }

    public Guid ActorUserId { get; init; }

    public Guid? InitialAdministratorUserId { get; init; }

    public string? InitialAdministratorEmail { get; init; }

    public IReadOnlyList<string> ActorPlatformRoles { get; init; } = [];
}
