using Kyvo.Domain.Entities;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class IdentityProviderRepository : IIdentityProviderRepository
{
    private readonly ApplicationDbContext _context;

    public IdentityProviderRepository(ApplicationDbContext context) => _context = context;

    public Task AddAsync(IdentityProvider provider, CancellationToken ct = default)
    {
        return _context.IdentityProviders
            .AddAsync(provider, ct)
            .AsTask();
    }

    public Task<IdentityProvider?> GetByAliasAsync(string alias, CancellationToken ct = default)
    {
        var normalized = alias.Trim().ToLowerInvariant();
        return _context.IdentityProviders
            .FirstOrDefaultAsync(x => x.Alias == normalized, ct);
    }

    public Task<IdentityProvider?> GetEnabledByAliasAsync(string alias, CancellationToken ct = default)
    {
        var normalized = alias.Trim().ToLowerInvariant();
        return _context.IdentityProviders
            .FirstOrDefaultAsync(x => x.Alias == normalized && x.Enabled, ct);
    }

    public Task<IdentityProvider?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        return _context.IdentityProviders
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<IdentityProvider>> ListEnabledAsync(CancellationToken ct = default)
    {
        return await _context.IdentityProviders
            .Where(x => x.Enabled)
            .OrderBy(x => x.Alias)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<IdentityProvider>> ListEnabledByCapabilityAsync(IdpCapability capability, CancellationToken ct = default)
    {
        // Filtering happens in memory after loading enabled providers because the value-converted
        // collection (IReadOnlyCollection<IdpCapability>) does not translate to SQL reliably across
        // EF Core providers. The set of enabled providers is small (typically <10), so this is fine.
        var enabled = await _context.IdentityProviders
            .Where(x => x.Enabled)
            .OrderBy(x => x.Alias)
            .ToListAsync(ct);

        return enabled.Where(x => x.Capabilities.Contains(capability)).ToList();
    }

    public async Task<IReadOnlyList<IdentityProvider>> ListAllAsync(CancellationToken ct = default)
    {
        return await _context.IdentityProviders
            .OrderBy(x => x.Alias)
            .ToListAsync(ct);
    }

    public Task<bool> AliasAlreadyExistsAsync(string alias, CancellationToken ct = default)
    {
        var normalized = alias.Trim().ToLowerInvariant();
        return _context.IdentityProviders
            .AnyAsync(x => x.Alias == normalized, ct);
    }
}
