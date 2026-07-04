using Kyvo.AspNetCore;
using Microsoft.EntityFrameworkCore;
using PulseCrm.Api.Data;

namespace PulseCrm.Api.Services;

public sealed class PulseCrmTenantScope
{
    private readonly IKyvoUserContext _user;
    private readonly PulseCrmDbContext _db;

    public PulseCrmTenantScope(IKyvoUserContext user, PulseCrmDbContext db)
    {
        _user = user;
        _db = db;
    }

    public async Task<Guid?> EnsureAsync(CancellationToken cancellationToken = default)
    {
        if (_user.TenantId.HasValue)
        {
            return _user.TenantId;
        }

        if (_user.UserId is null)
        {
            return null;
        }

        var subscription = await _db.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == _user.UserId, cancellationToken);

        return subscription?.TenantId;
    }
}
