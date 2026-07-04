using Kyvo.Domain.Common;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Kyvo.Infrastructure.Identity;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private ApplicationUser? _trackedForUpdate;

    public UserRepository(ApplicationDbContext context) => _context = context;

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        var entity = UserMapper.ToNewPersistence(user);
        entity.SetCreatedAt();
        return _context.Users.AddAsync(entity, ct).AsTask();
    }

    public async Task<User?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        _trackedForUpdate = await _context.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        return _trackedForUpdate is null ? null : UserMapper.ToDomain(_trackedForUpdate);
    }

    public async Task SyncFromDomainAsync(User user, CancellationToken ct = default)
    {
        if (_trackedForUpdate is null || _trackedForUpdate.Id != user.Id)
        {
            _trackedForUpdate = await _context.Users.FirstOrDefaultAsync(x => x.Id == user.Id, ct);
        }

        if (_trackedForUpdate is null)
        {
            throw new InvalidOperationException($"User '{user.Id}' is not loaded for update.");
        }

        UserMapper.ApplyToPersistence(user, _trackedForUpdate);
        _trackedForUpdate.SetUpdatedAt();
        _trackedForUpdate = null;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var entity = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalized, ct);

        return entity is null ? null : UserMapper.ToDomain(entity);
    }

    public Task<bool> EmailAlreadyExistsAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.Users.AnyAsync(x => x.Email == normalized, ct);
    }
}
