namespace Kyvo.Application.Services.Auth;

public interface ITenantAccountEligibilityService
{
    Task EnsureCanDeleteAccountAsync(
        Guid applicationId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
