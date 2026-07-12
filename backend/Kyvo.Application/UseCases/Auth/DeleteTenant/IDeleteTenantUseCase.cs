namespace Kyvo.Application.UseCases.Auth.DeleteTenant;

public interface IDeleteTenantUseCase
{
    Task ExecuteAsync(Guid tenantId, CancellationToken ct = default);
}
