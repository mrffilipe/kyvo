using System.Collections.Immutable;
using System.Security.Claims;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using OpenIddict.Abstractions;

namespace Kyvo.Infrastructure.Services.Oidc;

public sealed class KyvoClaimsPrincipalFactory : IKyvoClaimsPrincipalFactory
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly IUserPlatformRoleRepository _userPlatformRoles;

    public KyvoClaimsPrincipalFactory(
        ITenantMembershipRepository memberships,
        IUserPlatformRoleRepository userPlatformRoles)
    {
        _memberships = memberships;
        _userPlatformRoles = userPlatformRoles;
    }

    public async Task<ClaimsPrincipal> CreateAsync(User user, AuthSession session, string clientId, IReadOnlyList<string> scopes, CancellationToken ct = default)
    {
        var platformRoleAssignments = await _userPlatformRoles.ListByUserIdAsync(user.Id, ct);
        var platformRoles = platformRoleAssignments.Select(x => x.Role.Key).ToList();

        if (string.Equals(clientId, PlatformDefaults.AdminConsole.CLIENT_ID, StringComparison.Ordinal) &&
            !platformRoles.Contains(PlatformRoleDefaults.PLATFORM_ADMINISTRATOR, StringComparer.OrdinalIgnoreCase))
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.OAuthClient.PLATFORM_ADMIN_CONSOLE_ACCESS_DENIED);
        }

        var memberships = await _memberships.ListByUserIdWithTenantAndRolesAsync(user.Id, ct);
        var activeMembership = memberships.FirstOrDefault(m => m.Id == session.MembershipId);
        var tenantRoles = activeMembership?.Roles.Select(r => r.Role.Key.Value).ToList() ?? [];

        var identity = new ClaimsIdentity(
            authenticationType: "OpenIddict",
            nameType: OpenIddictConstants.Claims.Name,
            roleType: "trole");

        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString("D"))
            .SetClaim("uid", user.Id.ToString("D"))
            .SetClaim("sid", session.Id.ToString("D"))
            .SetClaim(OpenIddictConstants.Claims.Email, user.Email)
            .SetClaim(OpenIddictConstants.Claims.Name, user.DisplayName)
            .SetClaim("amr", "pwd");

        if (session.TenantId.HasValue)
        {
            identity.SetClaim("tid", session.TenantId.Value.ToString("D"));
        }

        if (session.MembershipId.HasValue)
        {
            identity.SetClaim("mid", session.MembershipId.Value.ToString("D"));
        }

        identity.SetClaims("trole", tenantRoles
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length > 0)
            .Distinct()
            .ToImmutableArray());

        identity.SetClaims(PlatformRoleDefaults.CLAIM_TYPE, platformRoles
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length > 0)
            .Distinct()
            .ToImmutableArray());

        identity.SetScopes(scopes.ToImmutableArray());
        identity.SetDestinations(GetDestinations);

        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            "tid" or "mid" or "trole" or "uid" or "sid" or PlatformRoleDefaults.CLAIM_TYPE or "amr" =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Subject or OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Email =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            _ => [OpenIddictConstants.Destinations.AccessToken]
        };
    }
}
