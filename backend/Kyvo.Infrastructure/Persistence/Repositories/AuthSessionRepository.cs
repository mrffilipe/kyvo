using Kyvo.Domain.Entities;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class AuthSessionRepository : IAuthSessionRepository
{
    private readonly ApplicationDbContext _context;

    public AuthSessionRepository(ApplicationDbContext context) => _context = context;

    public Task AddAsync(AuthSession session, CancellationToken ct = default)
    {
        return _context.AuthSessions
            .AddAsync(session, ct)
            .AsTask();
    }

    public Task<AuthSession?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        return _context.AuthSessions
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<AuthSession>> ListActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.AuthSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == SessionStatus.Active)
            .OrderByDescending(x => x.LastActivityAt)
            .ToListAsync(ct);
    }
}
