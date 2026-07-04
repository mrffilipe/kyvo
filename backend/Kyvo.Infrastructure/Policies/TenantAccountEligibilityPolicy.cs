using Kyvo.Application.Exceptions;
using Kyvo.Application.Policies;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Policies;

public sealed class TenantAccountEligibilityPolicy : ITenantAccountEligibilityPolicy
{
    private readonly IApplicationTenantRepository _applicationTenants;

    public TenantAccountEligibilityPolicy(IApplicationTenantRepository applicationTenants)
    {
        _applicationTenants = applicationTenants;
    }

    public async Task EnsureCanDeleteAccountAsync(Guid applicationId, Guid tenantId, CancellationToken ct = default)
    {
        var applicationTenant = await _applicationTenants.GetByApplicationAndTenantAsync(applicationId, tenantId, ct)
            ?? throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.APPLICATION_TENANT_NOT_FOUND);

        if (!applicationTenant.IsActive)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.ACCOUNT_DELETION_BLOCKED);
        }
    }
}
