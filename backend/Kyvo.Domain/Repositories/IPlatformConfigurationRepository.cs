using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface IPlatformConfigurationRepository
{
    Task AddAsync(PlatformConfiguration platformConfiguration, CancellationToken ct = default);
    Task<PlatformConfiguration?> GetForUpdateAsync(CancellationToken ct = default);
}
