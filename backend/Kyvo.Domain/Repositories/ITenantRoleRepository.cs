using Kyvo.Domain.Entities;
using Kyvo.Domain.ValueObjects;

namespace Kyvo.Domain.Repositories;

public interface ITenantRoleRepository
{
    Task AddAsync(TenantRole role, CancellationToken ct = default);
    Task<TenantRole?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TenantRole>> ListByTenantIdAsync(Guid tenantId, bool includeInactive = false, CancellationToken ct = default);
    Task<IReadOnlyList<TenantRole>> ListByTenantIdAndKeysAsync(
        Guid tenantId,
        IReadOnlyCollection<TenantRoleKey> keys,
        bool activeOnly = true,
        CancellationToken ct = default);
    Task<bool> KeyAlreadyExistsAsync(Guid tenantId, TenantRoleKey key, CancellationToken ct = default);
    Task<bool> HasActiveAssignmentsAsync(Guid roleId, CancellationToken ct = default);
    Task DeleteAsync(TenantRole role, CancellationToken ct = default);
}
