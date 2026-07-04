namespace Kyvo.Application.Shared;

public interface ITenantProvisioner
{
    Task<TenantProvisionResult> ProvisionAsync(TenantProvisionRequest request, CancellationToken ct = default);
}
