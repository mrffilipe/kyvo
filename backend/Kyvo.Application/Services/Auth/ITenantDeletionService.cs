namespace Kyvo.Application.Services.Auth;

public interface ITenantDeletionService
{
    Task DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
