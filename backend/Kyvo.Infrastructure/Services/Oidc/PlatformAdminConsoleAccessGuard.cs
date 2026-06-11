using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.Oidc;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Services.Oidc;

public sealed class PlatformAdminConsoleAccessGuard : IPlatformAdminConsoleAccessGuard
{
    private readonly IUserPlatformRoleRepository _userPlatformRoles;

    public PlatformAdminConsoleAccessGuard(IUserPlatformRoleRepository userPlatformRoles) => _userPlatformRoles = userPlatformRoles;

    public async Task<OidcError?> TryValidateAccessAsync(Guid userId, string clientId, CancellationToken ct = default)
    {
        if (!string.Equals(clientId, PlatformDefaults.AdminConsole.ClientId, StringComparison.Ordinal))
        {
            return null;
        }

        var assignments = await _userPlatformRoles.ListByUserIdAsync(userId, ct);
        var isPlatformAdministrator = assignments.Any(x => string.Equals(x.Role.Key, PlatformRoleDefaults.PlatformAdministrator, StringComparison.OrdinalIgnoreCase));

        if (isPlatformAdministrator)
        {
            return null;
        }

        return new OidcError
        {
            Error = OidcConstants.Errors.AccessDenied,
            ErrorDescription = ApplicationErrorMessages.OAuthClient.PlatformAdminConsoleAccessDenied
        };
    }
}
