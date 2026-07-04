namespace Kyvo.Application.Policies;

public interface ITenantAccountEligibilityPolicy
{
    Task EnsureCanDeleteAccountAsync(Guid applicationId, Guid tenantId, CancellationToken ct = default);
}
