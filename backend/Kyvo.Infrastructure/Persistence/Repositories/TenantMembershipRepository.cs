using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class TenantMembershipRepository : ITenantMembershipRepository
{
    private readonly ApplicationDbContext _context;

    public TenantMembershipRepository(ApplicationDbContext context) => _context = context;

    public Task AddAsync(TenantMembership membership, CancellationToken ct = default)
    {
        return _context.TenantMemberships
            .AddAsync(membership, ct)
            .AsTask();
    }

    public Task<TenantMembership?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        return _context.TenantMemberships
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<TenantMembership?> GetByUserIdAndTenantIdWithRolesAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        return _context.TenantMemberships
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId, ct);
    }

    public async Task<IReadOnlyList<TenantMembership>> ListByUserIdWithTenantAndRolesAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.TenantMemberships
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync(ct);
    }

    public Task<bool> HasActiveMembershipAsync(Guid userId, CancellationToken ct = default)
    {
        return _context.TenantMemberships
            .AnyAsync(x => x.UserId == userId && x.IsActive, ct);
    }
}
