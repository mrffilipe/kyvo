namespace Kyvo.Application.Services.Tenancy;

/// <summary>
/// Holds the current tenant id for EF query filters (from tenant JWT claim <c>tid</c>).
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
    void SetTenant(Guid? tenantId);
}

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void SetTenant(Guid? tenantId) => TenantId = tenantId;
}
