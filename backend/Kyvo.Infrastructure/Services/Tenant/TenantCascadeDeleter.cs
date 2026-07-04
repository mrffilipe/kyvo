using Kyvo.Application.Ports.Tenants;
using Kyvo.Application.Services.TenantResolutionCache;
using Kyvo.Domain.Exceptions;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Services.Tenant;

public sealed class TenantCascadeDeleter : ITenantCascadeDeleter
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantResolutionCache _tenantResolutionCache;

    public TenantCascadeDeleter(ApplicationDbContext context, ITenantResolutionCache tenantResolutionCache)
    {
        _context = context;
        _tenantResolutionCache = tenantResolutionCache;
    }

    public async Task DeleteAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => new { x.Id, Key = x.Key.Value })
            .FirstOrDefaultAsync(ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.Tenant.TENANT_NOT_FOUND);

        // Access/refresh tokens issued by OpenIddict are keyed by subject+application, not by AuthSession, so
        // there is nothing to clean up there directly: once the session row below is gone, /connect/token
        // refresh attempts for it are rejected (AuthorizationController.Exchange requires an active session),
        // and previously issued access tokens simply expire on their own short TTL.
        await _context.AuthSessions
            .Where(x => x.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);

        var inviteIds = await _context.TenantInvites
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (inviteIds.Count > 0)
        {
            await _context.TenantInviteRoles
                .Where(x => inviteIds.Contains(x.InviteId))
                .ExecuteDeleteAsync(ct);
        }

        await _context.TenantInvites
            .Where(x => x.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);

        var membershipIds = await _context.TenantMemberships
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (membershipIds.Count > 0)
        {
            await _context.TenantMembershipRoles
                .Where(x => membershipIds.Contains(x.MembershipId))
                .ExecuteDeleteAsync(ct);
        }

        await _context.TenantMemberships
            .Where(x => x.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);

        await _context.TenantRoles
            .Where(x => x.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);

        await _context.ApplicationTenants
            .Where(x => x.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);

        await _context.Tenants
            .Where(x => x.Id == tenantId)
            .ExecuteDeleteAsync(ct);

        await _tenantResolutionCache.InvalidateByIdentifierAsync(tenant.Key, ct);
    }
}
