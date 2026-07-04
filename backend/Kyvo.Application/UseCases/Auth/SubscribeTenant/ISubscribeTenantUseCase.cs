using Kyvo.Application.UseCases.Auth;

namespace Kyvo.Application.UseCases.Auth.SubscribeTenant;

public interface ISubscribeTenantUseCase
{
    Task<TenantContextResult> ExecuteAsync(SubscribeTenantRequest request, CancellationToken ct = default);
}
