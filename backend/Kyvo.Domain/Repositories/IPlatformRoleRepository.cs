using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface IPlatformRoleRepository
{
    Task AddAsync(PlatformRole role, CancellationToken ct = default);
    Task<PlatformRole?> GetByKeyAsync(string key, CancellationToken ct = default);
}
