using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface ITenantMembershipRepository
{
    Task AddAsync(TenantMembership membership, CancellationToken ct = default);
    Task<TenantMembership?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<TenantMembership?> GetByUserIdAndTenantIdWithRolesAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantMembership>> ListByUserIdWithTenantAndRolesAsync(Guid userId, CancellationToken ct = default);
    Task<bool> HasActiveMembershipAsync(Guid userId, CancellationToken ct = default);
}
