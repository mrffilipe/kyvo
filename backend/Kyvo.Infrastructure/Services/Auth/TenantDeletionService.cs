using Kyvo.Application.Services.Auth;
using Kyvo.Application.Services.TenantResolutionCache;
using Kyvo.Domain.Exceptions;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Services.Auth;

public sealed class TenantDeletionService : ITenantDeletionService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantResolutionCache _tenantResolutionCache;

    public TenantDeletionService(
        ApplicationDbContext context,
        ITenantResolutionCache tenantResolutionCache)
    {
        _context = context;
        _tenantResolutionCache = tenantResolutionCache;
    }

    public async Task DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => new { x.Id, Key = x.Key.Value })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainNotFoundException(DomainErrorMessages.Tenant.TenantNotFound);

        var sessionIds = await _context.AuthSessions
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (sessionIds.Count > 0)
        {
            await _context.OidcRefreshTokens
                .Where(x => sessionIds.Contains(x.AuthSessionId))
                .ExecuteDeleteAsync(cancellationToken);

            await _context.OidcAuthorizationCodes
                .Where(x => sessionIds.Contains(x.AuthSessionId))
                .ExecuteDeleteAsync(cancellationToken);

            await _context.AuthSessions
                .Where(x => x.TenantId == tenantId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        var inviteIds = await _context.TenantInvites
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (inviteIds.Count > 0)
        {
            await _context.TenantInviteRoles
                .Where(x => inviteIds.Contains(x.InviteId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _context.TenantInvites
            .Where(x => x.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken);

        var membershipIds = await _context.TenantMemberships
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (membershipIds.Count > 0)
        {
            await _context.TenantMembershipRoles
                .Where(x => membershipIds.Contains(x.MembershipId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _context.TenantMemberships
            .Where(x => x.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.TenantRoles
            .Where(x => x.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.ApplicationTenants
            .Where(x => x.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.Tenants
            .Where(x => x.Id == tenantId)
            .ExecuteDeleteAsync(cancellationToken);

        await _tenantResolutionCache.InvalidateByIdentifierAsync(tenant.Key, cancellationToken);
    }
}
