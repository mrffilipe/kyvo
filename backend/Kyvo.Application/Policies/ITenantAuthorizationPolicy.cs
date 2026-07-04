namespace Kyvo.Application.Policies;

public interface ITenantAuthorizationPolicy
{
    Task EnsureTenantAdministrativeAccessAsync(
        Guid tenantId,
        Guid actorUserId,
        IReadOnlyCollection<string> actorPlatformRoles,
        CancellationToken ct = default);
}
