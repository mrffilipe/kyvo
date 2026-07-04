using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _context;

    public TenantRepository(ApplicationDbContext context) => _context = context;

    public Task AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        return _context.Tenants
            .AddAsync(tenant, ct)
            .AsTask();
    }

    public Task<Tenant?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        return _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<bool> KeyAlreadyExistsAsync(string key, CancellationToken ct = default)
    {
        var normalized = key.Trim().ToLowerInvariant();
        return _context.Tenants
            .AnyAsync(x => x.Key.Value == normalized, ct);
    }
}
