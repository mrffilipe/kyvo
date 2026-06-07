using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface IPlatformRoleRepository
{
    Task AddAsync(PlatformRole role, CancellationToken cancellationToken = default);
    Task<PlatformRole?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
}
