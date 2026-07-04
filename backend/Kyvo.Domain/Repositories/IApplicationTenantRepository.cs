using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface IApplicationTenantRepository
{
    Task AddAsync(ApplicationTenant applicationTenant, CancellationToken ct = default);
    Task<ApplicationTenant?> GetByApplicationAndTenantAsync(Guid applicationId, Guid tenantId, CancellationToken ct = default);
    Task<bool> MappingAlreadyExistsAsync(Guid applicationId, Guid tenantId, CancellationToken ct = default);
}
