namespace Kyvo.Application.UseCases.Tenants.UpdateTenant;

public interface IUpdateTenantUseCase
{
    Task ExecuteAsync(UpdateTenantRequest request, CancellationToken ct = default);
}
