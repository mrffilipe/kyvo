using Kyvo.Application.Common;
using Kyvo.Application.Queries.Tenants.CheckTenantKeyAvailability;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;

namespace Kyvo.Infrastructure.Queries.Tenants;

public sealed class CheckTenantKeyAvailabilityQuery : ICheckTenantKeyAvailabilityQuery
{
    private readonly ITenantRepository _tenants;

    public CheckTenantKeyAvailabilityQuery(ITenantRepository tenants) => _tenants = tenants;

    public async Task<AvailabilityDto> ExecuteAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new AvailabilityDto { Available = false };
        }

        try
        {
            var tenantKey = new TenantKey(key);
            var exists = await _tenants.KeyAlreadyExistsAsync(tenantKey, ct);
            return new AvailabilityDto { Available = !exists };
        }
        catch (DomainValidationException)
        {
            return new AvailabilityDto { Available = false };
        }
    }
}
