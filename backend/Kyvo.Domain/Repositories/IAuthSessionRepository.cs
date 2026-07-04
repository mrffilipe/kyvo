using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface IAuthSessionRepository
{
    Task AddAsync(AuthSession session, CancellationToken ct = default);
    Task<AuthSession?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AuthSession>> ListActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<AuthSession>> ListActiveByUserIdAndTenantIdForUpdateAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
}
