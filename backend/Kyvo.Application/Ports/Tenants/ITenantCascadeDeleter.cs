namespace Kyvo.Application.Ports.Tenants;

public interface ITenantCascadeDeleter
{
    Task DeleteAsync(Guid tenantId, CancellationToken ct = default);
}
