using System.Collections.Immutable;
using System.Security.Claims;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using OpenIddict.Abstractions;

namespace Kyvo.Infrastructure.Oidc;

/// <summary>
/// Pure OIDC claims: sub, email, name, sid, prole, client_id — no tenant context.
/// </summary>
public sealed class OidcClaimsPrincipalFactory : IOidcClaimsPrincipalFactory
{
    private readonly IUserPlatformRoleRepository _userPlatformRoles;

    public OidcClaimsPrincipalFactory(IUserPlatformRoleRepository userPlatformRoles)
    {
        _userPlatformRoles = userPlatformRoles;
    }

    public async Task<ClaimsPrincipal> CreateAsync(
        User user,
        AuthSession session,
        string clientId,
        IReadOnlyList<string> scopes,
        CancellationToken ct = default)
    {
        var platformRoleAssignments = await _userPlatformRoles.ListByUserIdAsync(user.Id, ct);
        var platformRoles = platformRoleAssignments.Select(x => x.Role.Key).ToList();

        if (string.Equals(clientId, PlatformDefaults.AdminConsole.CLIENT_ID, StringComparison.Ordinal) &&
            !platformRoles.Contains(PlatformRoleDefaults.PLATFORM_ADMINISTRATOR, StringComparer.OrdinalIgnoreCase))
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.OAuthClient.PLATFORM_ADMIN_CONSOLE_ACCESS_DENIED);
        }

        var identity = new ClaimsIdentity(
            authenticationType: "OpenIddict",
            nameType: OpenIddictConstants.Claims.Name,
            roleType: PlatformRoleDefaults.CLAIM_TYPE);

        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString("D"))
            .SetClaim(OpenIddictConstants.Claims.Email, user.Email)
            .SetClaim(OpenIddictConstants.Claims.Name, user.DisplayName)
            .SetClaim("sid", session.Id.ToString("D"))
            .SetClaim("client_id", clientId);

        identity.SetClaims(PlatformRoleDefaults.CLAIM_TYPE, platformRoles
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length > 0)
            .Distinct()
            .ToImmutableArray());

        var scopeArray = scopes.ToImmutableArray();
        identity.SetScopes(scopeArray);
        identity.SetDestinations(claim => GetDestinations(claim, scopeArray));

        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ImmutableArray<string> scopes)
    {
        var hasProfile = scopes.Contains(OpenIddictConstants.Scopes.Profile, StringComparer.Ordinal);
        var hasEmail = scopes.Contains(OpenIddictConstants.Scopes.Email, StringComparer.Ordinal);

        return claim.Type switch
        {
            OpenIddictConstants.Claims.Name =>
                hasProfile
                    ? [OpenIddictConstants.Destinations.IdentityToken, OpenIddictConstants.Destinations.AccessToken]
                    : [OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Email =>
                hasEmail
                    ? [OpenIddictConstants.Destinations.IdentityToken, OpenIddictConstants.Destinations.AccessToken]
                    : [OpenIddictConstants.Destinations.IdentityToken],
            "sid" or PlatformRoleDefaults.CLAIM_TYPE or "client_id" =>
                [OpenIddictConstants.Destinations.AccessToken],
            OpenIddictConstants.Claims.Subject =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            _ => [OpenIddictConstants.Destinations.AccessToken]
        };
    }
}
