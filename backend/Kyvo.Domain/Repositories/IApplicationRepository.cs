using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface IApplicationRepository
{
    Task AddAsync(Application application, CancellationToken ct = default);
    Task<Application?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Application?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<bool> SlugAlreadyExistsAsync(string slug, CancellationToken ct = default);
}
