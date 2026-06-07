using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class PlatformRoleRepository : IPlatformRoleRepository
{
    private readonly ApplicationDbContext _context;

    public PlatformRoleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(PlatformRole role, CancellationToken cancellationToken = default)
    {
        return _context.PlatformRoles
            .AddAsync(role, cancellationToken)
            .AsTask();
    }

    public Task<PlatformRole?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim().ToLowerInvariant();
        return _context.PlatformRoles
            .FirstOrDefaultAsync(x => x.Key == normalized, cancellationToken);
    }
}
