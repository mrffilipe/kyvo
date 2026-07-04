using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class TenantInviteRepository : ITenantInviteRepository
{
    private readonly ApplicationDbContext _context;

    public TenantInviteRepository(ApplicationDbContext context) => _context = context;

    public Task AddAsync(TenantInvite invite, CancellationToken ct = default)
    {
        return _context.Set<TenantInvite>()
            .AddAsync(invite, ct)
            .AsTask();
    }

    public Task<TenantInvite?> GetByTokenHashWithRolesAsync(string tokenHash, CancellationToken ct = default)
    {
        return _context.Set<TenantInvite>()
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
    }

    public Task<TenantInvite?> GetForUpdateAsync(Guid inviteId, CancellationToken ct = default)
    {
        return _context.Set<TenantInvite>()
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == inviteId, ct);
    }

    public async Task<(IReadOnlyList<TenantInvite> Items, int TotalCount)> ListByTenantIdAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Set<TenantInvite>()
            .AsNoTracking()
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
