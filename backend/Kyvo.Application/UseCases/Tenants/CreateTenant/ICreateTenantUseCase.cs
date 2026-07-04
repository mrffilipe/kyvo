using Kyvo.Application.UseCases.Tenants.CreateTenant;

namespace Kyvo.Application.UseCases.Tenants.CreateTenant;

public interface ICreateTenantUseCase
{
    Task<Guid> ExecuteAsync(CreateTenantRequest request, CancellationToken ct = default);
}
