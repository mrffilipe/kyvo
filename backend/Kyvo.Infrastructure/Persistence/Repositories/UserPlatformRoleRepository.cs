using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class UserPlatformRoleRepository : IUserPlatformRoleRepository
{
    private readonly ApplicationDbContext _context;

    public UserPlatformRoleRepository(ApplicationDbContext context) => _context = context;

    public Task AddAsync(UserPlatformRole assignment, CancellationToken ct = default)
    {
        return _context.UserPlatformRoles
            .AddAsync(assignment, ct)
            .AsTask();
    }

    public async Task<IReadOnlyList<UserPlatformRole>> ListByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.UserPlatformRoles
            .Include(x => x.Role)
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
    }

    public Task<bool> AssignmentAlreadyExistsAsync(Guid userId, Guid roleId, CancellationToken ct = default)
    {
        return _context.UserPlatformRoles
            .AnyAsync(x => x.UserId == userId && x.RoleId == roleId, ct);
    }
}
