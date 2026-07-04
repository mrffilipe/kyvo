using Kyvo.Application.Common;

namespace Kyvo.Application.Queries.Tenants.CheckTenantKeyAvailability;

public interface ICheckTenantKeyAvailabilityQuery
{
    Task<AvailabilityDto> ExecuteAsync(string key, CancellationToken ct = default);
}
