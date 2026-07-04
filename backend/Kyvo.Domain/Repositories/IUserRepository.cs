using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken ct = default);
    Task<User?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
    Task SyncFromDomainAsync(User user, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailAlreadyExistsAsync(string email, CancellationToken ct = default);
}
