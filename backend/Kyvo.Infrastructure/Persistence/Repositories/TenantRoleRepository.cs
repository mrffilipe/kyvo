using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class TenantRoleRepository : ITenantRoleRepository
{
    private readonly ApplicationDbContext _context;

    public TenantRoleRepository(ApplicationDbContext context) => _context = context;

    public Task AddAsync(TenantRole role, CancellationToken ct = default)
    {
        return _context.TenantRoles
            .AddAsync(role, ct)
            .AsTask();
    }

    public Task<TenantRole?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        return _context.TenantRoles
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<TenantRole>> ListByTenantIdAsync(Guid tenantId, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _context.TenantRoles
            .Where(x => x.TenantId == tenantId);
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TenantRole>> ListByTenantIdAndKeysAsync(
        Guid tenantId,
        IReadOnlyCollection<TenantRoleKey> keys,
        bool activeOnly = true,
        CancellationToken ct = default)
    {
        var values = keys.Select(x => x.Value).ToList();
        var query = _context.TenantRoles
            .Where(x => x.TenantId == tenantId && values.Contains(x.Key.Value));

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .ToListAsync(ct);
    }

    public Task<bool> KeyAlreadyExistsAsync(Guid tenantId, TenantRoleKey key, CancellationToken ct = default)
    {
        return _context.TenantRoles
            .AnyAsync(x => x.TenantId == tenantId && x.Key.Value == key.Value, ct);
    }

    public Task<bool> HasActiveAssignmentsAsync(Guid roleId, CancellationToken ct = default)
    {
        return (
            from membershipRole in _context.TenantMembershipRoles.AsNoTracking()
            join membership in _context.TenantMemberships.AsNoTracking()
                on membershipRole.MembershipId equals membership.Id
            where membershipRole.RoleId == roleId && membership.IsActive
            select membershipRole).AnyAsync(ct);
    }

    public Task DeleteAsync(TenantRole role, CancellationToken ct = default)
    {
        _context.TenantRoles.Remove(role);
        return Task.CompletedTask;
    }
}
