using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface ITenantRepository
{
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
    Task<Tenant?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<bool> KeyAlreadyExistsAsync(string key, CancellationToken ct = default);
}
