using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface ITenantInviteRepository
{
    Task AddAsync(TenantInvite invite, CancellationToken ct = default);
    Task<TenantInvite?> GetByTokenHashWithRolesAsync(string tokenHash, CancellationToken ct = default);
    Task<TenantInvite?> GetForUpdateAsync(Guid inviteId, CancellationToken ct = default);
    Task<(IReadOnlyList<TenantInvite> Items, int TotalCount)> ListByTenantIdAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
