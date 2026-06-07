using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface ITenantInviteRepository
{
    Task AddAsync(TenantInvite invite, CancellationToken cancellationToken = default);
    Task<TenantInvite?> GetByTokenHashWithRolesAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<TenantInvite?> GetForUpdateAsync(Guid inviteId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<TenantInvite> Items, int TotalCount)> ListByTenantIdAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default);
}
