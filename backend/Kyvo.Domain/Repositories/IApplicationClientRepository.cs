using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface IApplicationClientRepository
{
    Task AddAsync(ApplicationClient client, CancellationToken ct = default);
    Task<ApplicationClient?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApplicationClient?> GetByClientIdAsync(string clientId, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationClient>> ListByApplicationIdAsync(Guid applicationId, CancellationToken ct = default);
}
