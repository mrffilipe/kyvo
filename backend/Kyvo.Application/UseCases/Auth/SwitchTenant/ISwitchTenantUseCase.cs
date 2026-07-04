using Kyvo.Application.UseCases.Auth;

namespace Kyvo.Application.UseCases.Auth.SwitchTenant;

public interface ISwitchTenantUseCase
{
    Task<TenantContextResult> ExecuteAsync(SwitchTenantRequest request, CancellationToken ct = default);
}
