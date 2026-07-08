using System.Security.Claims;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Kyvo.Infrastructure.Oidc;

public sealed class ValidateAuthSessionHandler : IOpenIddictServerHandler<ProcessSignInContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
            .UseScopedHandler<ValidateAuthSessionHandler>()
            .SetOrder(10_000)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    private readonly IAuthSessionRepository _sessions;

    public ValidateAuthSessionHandler(IAuthSessionRepository sessions) => _sessions = sessions;

    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.Principal is null)
        {
            return;
        }

        var sidValue = context.Principal.FindFirst("sid")?.Value;
        if (!Guid.TryParse(sidValue, out var sessionId))
        {
            context.Reject(
                error: OpenIddictConstants.Errors.InvalidGrant,
                description: ApplicationErrorMessages.OAuthAuthorization.MISSING_LOGIN_CONTEXT);
            return;
        }

        var session = await _sessions.GetForUpdateAsync(sessionId, context.CancellationToken);
        if (session is null || session.Status != SessionStatus.Active)
        {
            context.Reject(
                error: context.EndpointType == OpenIddictServerEndpointType.Authorization
                    ? OpenIddictConstants.Errors.LoginRequired
                    : OpenIddictConstants.Errors.InvalidGrant,
                description: ApplicationErrorMessages.OAuthAuthorization.SESSION_NO_LONGER_ACTIVE);
        }
    }
}

public sealed class ValidateAdminConsoleAccessHandler : IOpenIddictServerHandler<ProcessSignInContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
            .UseScopedHandler<ValidateAdminConsoleAccessHandler>()
            .SetOrder(10_100)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    private readonly IUserPlatformRoleRepository _userPlatformRoles;

    public ValidateAdminConsoleAccessHandler(IUserPlatformRoleRepository userPlatformRoles)
    {
        _userPlatformRoles = userPlatformRoles;
    }

    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.EndpointType != OpenIddictServerEndpointType.Authorization || context.Principal is null)
        {
            return;
        }

        var clientId = context.Request?.ClientId;
        if (!string.Equals(clientId, PlatformDefaults.AdminConsole.CLIENT_ID, StringComparison.Ordinal))
        {
            return;
        }

        if (HasPlatformAdminClaim(context.Principal))
        {
            return;
        }

        var userIdValue = context.Principal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        if (Guid.TryParse(userIdValue, out var userId))
        {
            var assignments = await _userPlatformRoles.ListByUserIdAsync(userId, context.CancellationToken);
            if (assignments.Any(x => string.Equals(
                    x.Role.Key,
                    PlatformRoleDefaults.PLATFORM_ADMINISTRATOR,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
        }

        context.Reject(
            error: OpenIddictConstants.Errors.AccessDenied,
            description: ApplicationErrorMessages.OAuthClient.PLATFORM_ADMIN_CONSOLE_ACCESS_DENIED);
    }

    private static bool HasPlatformAdminClaim(ClaimsPrincipal principal) =>
        principal.FindAll(PlatformRoleDefaults.CLAIM_TYPE)
            .Any(claim => string.Equals(claim.Value, PlatformRoleDefaults.PLATFORM_ADMINISTRATOR, StringComparison.OrdinalIgnoreCase));
}

public sealed class ApplyClientAccessTokenLifetimeHandler : IOpenIddictServerHandler<ProcessSignInContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
            .UseScopedHandler<ApplyClientAccessTokenLifetimeHandler>()
            .SetOrder(50_000)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    private readonly ApplicationDbContext _dbContext;

    public ApplyClientAccessTokenLifetimeHandler(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.Principal is null)
        {
            return;
        }

        var clientId = context.Request?.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        var ttlSeconds = await _dbContext.Set<Persistence.Entities.KyvoOpenIddictApplication>()
            .AsNoTracking()
            .Where(x => x.ClientId == clientId)
            .Select(x => x.AccessTokenTtlSeconds)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (ttlSeconds > 0)
        {
            context.Principal.SetAccessTokenLifetime(TimeSpan.FromSeconds(ttlSeconds));
        }
    }
}

public sealed class TouchAuthSessionHandler : IOpenIddictServerHandler<ProcessSignInContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; } =
        OpenIddictServerHandlerDescriptor.CreateBuilder<ProcessSignInContext>()
            .UseScopedHandler<TouchAuthSessionHandler>()
            .SetOrder(60_000)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    private readonly IAuthSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;

    public TouchAuthSessionHandler(IAuthSessionRepository sessions, IUnitOfWork unitOfWork)
    {
        _sessions = sessions;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.EndpointType != OpenIddictServerEndpointType.Token || context.Principal is null)
        {
            return;
        }

        var sidValue = context.Principal.FindFirst("sid")?.Value;
        if (!Guid.TryParse(sidValue, out var sessionId))
        {
            return;
        }

        var session = await _sessions.GetForUpdateAsync(sessionId, context.CancellationToken);
        if (session is null)
        {
            return;
        }

        session.Touch();
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}
