using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.Auth;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Services.Auth;

public sealed class TenantAccountEligibilityService : ITenantAccountEligibilityService
{
    private readonly IApplicationTenantRepository _applicationTenants;

    public TenantAccountEligibilityService(IApplicationTenantRepository applicationTenants)
    {
        _applicationTenants = applicationTenants;
    }

    public async Task EnsureCanDeleteAccountAsync(
        Guid applicationId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var applicationTenant = await _applicationTenants.GetByApplicationAndTenantAsync(applicationId, tenantId, cancellationToken)
            ?? throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.ApplicationTenantNotFound);

        if (!applicationTenant.IsActive)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.AccountDeletionBlocked);
        }
    }
}
