using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Kyvo.Domain.Constants;
using Microsoft.IdentityModel.Tokens;

namespace Kyvo.API.Tests.Fixtures;

internal static class TestAccessTokenFactory
{
    private static readonly Lazy<RSA> SigningRsa = new(() =>
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(TestSigningKey.Pem);
        return rsa;
    });

    public static string CreatePlatformToken(Guid userId, Guid sessionId, string clientId = "test-client")
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString("D")),
            new("sid", sessionId.ToString("D")),
            new("client_id", clientId),
        };

        return WriteToken(claims);
    }

    public static string CreateTenantToken(
        Guid userId,
        Guid sessionId,
        Guid tenantId,
        Guid membershipId,
        params string[] tenantRoles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString("D")),
            new("sid", sessionId.ToString("D")),
            new("token_use", "tenant"),
            new("tid", tenantId.ToString("D")),
            new("mid", membershipId.ToString("D")),
        };

        foreach (var role in tenantRoles.Length > 0 ? tenantRoles : [TenantRoleDefaults.OWNER])
        {
            claims.Add(new Claim("trole", role));
        }

        return WriteToken(claims);
    }

    private static string WriteToken(IEnumerable<Claim> claims)
    {
        var credentials = new SigningCredentials(
            new RsaSecurityKey(SigningRsa.Value),
            SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: TestSigningKey.Issuer,
            audience: TestSigningKey.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
