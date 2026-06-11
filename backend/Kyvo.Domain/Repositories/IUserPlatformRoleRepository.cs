using Kyvo.Domain.Entities;

namespace Kyvo.Domain.Repositories;

public interface IUserPlatformRoleRepository
{
    Task AddAsync(UserPlatformRole assignment, CancellationToken ct = default);
    Task<IReadOnlyList<UserPlatformRole>> ListByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> AssignmentAlreadyExistsAsync(Guid userId, Guid roleId, CancellationToken ct = default);
}
