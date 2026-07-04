namespace Kyvo.Application.UseCases.Applications.ProvisionTenant;

public interface IProvisionTenantUseCase
{
    Task<ProvisionApplicationTenantResult> ExecuteAsync(ProvisionApplicationTenantRequest request, CancellationToken ct = default);
}
