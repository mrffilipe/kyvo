using System.Security.Claims;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Kyvo.Infrastructure.Identity;

/// <summary>
/// Standard ASP.NET Identity claims principal factory. Adds platform role claims (prole) and OIDC profile claims.
/// </summary>
public sealed class KyvoUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
{
    private readonly IUserPlatformRoleRepository _userPlatformRoles;

    public KyvoUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> options,
        IUserPlatformRoleRepository userPlatformRoles)
        : base(userManager, roleManager, options)
    {
        _userPlatformRoles = userPlatformRoles;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString("D"));

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            identity.SetClaim(OpenIddictConstants.Claims.Email, user.Email);
        }

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            identity.SetClaim(OpenIddictConstants.Claims.Name, user.DisplayName);
        }

        var platformRoleAssignments = await _userPlatformRoles.ListByUserIdAsync(user.Id);
        foreach (var role in platformRoleAssignments
                     .Select(x => x.Role.Key.Trim().ToLowerInvariant())
                     .Where(x => x.Length > 0)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            identity.AddClaim(new Claim(PlatformRoleDefaults.CLAIM_TYPE, role));
        }

        return identity;
    }
}
