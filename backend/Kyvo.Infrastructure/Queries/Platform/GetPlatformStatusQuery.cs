using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Queries.Platform.GetPlatformStatus;
using Kyvo.Domain.Constants;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Queries.Platform;

public sealed class GetPlatformStatusQuery : IGetPlatformStatusQuery
{
    private readonly ApplicationDbContext _context;
    private readonly IOAuthClientManager _oauthClients;

    public GetPlatformStatusQuery(ApplicationDbContext context, IOAuthClientManager oauthClients)
    {
        _context = context;
        _oauthClients = oauthClients;
    }

    public async Task<PlatformStatusResult> ExecuteAsync(CancellationToken ct = default)
    {
        var configuration = await _context.PlatformConfigurations
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var isConfigured = configuration?.IsBootstrapped == true && configuration.RootUserId.HasValue;

        if (isConfigured)
        {
            var client = await _oauthClients.GetByClientIdAsync(PlatformDefaults.AdminConsole.CLIENT_ID, ct);
            if (client is not null)
            {
                await _oauthClients.RepairAdminConsoleClientAsync(client.ApplicationId, ct);
            }
        }

        return new PlatformStatusResult
        {
            IsConfigured = isConfigured,
            RequiresBootstrap = !isConfigured,
            OauthClientId = isConfigured ? configuration?.OauthClientId : null
        };
    }
}
