using Kyvo.Application.Ports.Tenants;
using Kyvo.Application.UseCases.Auth.DeleteTenant;

namespace Kyvo.Application.UseCases.Auth;

public sealed class DeleteTenantUseCase : IDeleteTenantUseCase
{
    private readonly ITenantCascadeDeleter _tenantCascadeDeleter;

    public DeleteTenantUseCase(ITenantCascadeDeleter tenantCascadeDeleter)
    {
        _tenantCascadeDeleter = tenantCascadeDeleter;
    }

    public Task ExecuteAsync(Guid tenantId, CancellationToken ct = default)
        => _tenantCascadeDeleter.DeleteAsync(tenantId, ct);
}
