using System.IdentityModel.Tokens.Jwt;
using Kyvo.Application.Services.Oidc;
using Microsoft.IdentityModel.Tokens;

namespace Kyvo.API.Common;

internal static class IdTokenHintClientIdResolver
{
    public static string? TryResolveClientId(string? idTokenHint, IJwtSigningService jwtSigning)
    {
        if (string.IsNullOrWhiteSpace(idTokenHint))
        {
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var parameters = new TokenValidationParameters
            {
                ValidIssuer = jwtSigning.Issuer,
                IssuerSigningKey = jwtSigning.SigningKey,
                ValidateAudience = false,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = handler.ValidateToken(idTokenHint, parameters, out _);
            return principal.FindFirst("aud")?.Value;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
