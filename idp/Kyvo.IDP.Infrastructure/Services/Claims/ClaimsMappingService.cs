using System.Security.Claims;
using Kyvo.IDP.Application.Services.Claims;
using OpenIddict.Abstractions;

namespace Kyvo.IDP.Infrastructure.Services.Claims;

public sealed class ClaimsMappingService : IClaimsMappingService
{
    public MappedExternalIdentity Map(ClaimsPrincipal externalPrincipal, string provider)
    {
        var providerUserId = FindClaim(externalPrincipal,
            OpenIddictConstants.Claims.Subject,
            ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("External identity is missing subject.");

        var email = FindClaim(externalPrincipal,
            OpenIddictConstants.Claims.Email,
            ClaimTypes.Email)
            ?? throw new InvalidOperationException("External identity is missing email.");

        var emailVerifiedRaw = FindClaim(externalPrincipal, OpenIddictConstants.Claims.EmailVerified, "email_verified");
        var emailVerified = string.Equals(emailVerifiedRaw, "true", StringComparison.OrdinalIgnoreCase)
            || emailVerifiedRaw == "True";

        return new MappedExternalIdentity
        {
            Provider = provider,
            ProviderUserId = providerUserId,
            Email = email.Trim(),
            EmailVerified = emailVerified || string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase),
            Name = FindClaim(externalPrincipal, OpenIddictConstants.Claims.Name, ClaimTypes.Name),
            GivenName = FindClaim(externalPrincipal, OpenIddictConstants.Claims.GivenName, ClaimTypes.GivenName),
            FamilyName = FindClaim(externalPrincipal, OpenIddictConstants.Claims.FamilyName, ClaimTypes.Surname),
            Picture = FindClaim(externalPrincipal, OpenIddictConstants.Claims.Picture, "picture")
        };
    }

    private static string? FindClaim(ClaimsPrincipal principal, params string[] types) =>
        types.Select(principal.FindFirst).FirstOrDefault(c => c is not null)?.Value;
}
