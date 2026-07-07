using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Kyvo.Application.Configurations;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Domain.Entities;
using Kyvo.Infrastructure.Services.Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kyvo.Infrastructure.Oidc;

public class TenantAccessTokenIssuer : ITenantAccessTokenIssuer
{
    private readonly SigningCertificateProvider _signingKeys;
    private readonly IConfiguration _configuration;

    public TenantAccessTokenIssuer(SigningCertificateProvider signingKeys, IConfiguration configuration)
    {
        _signingKeys = signingKeys;
        _configuration = configuration;
    }

    public string IssueToken(AuthSession session, IEnumerable<string> platformRoles, IEnumerable<string> tenantRoles)
    {
        var jwtOptions = _configuration.GetSection(JwtOptions.SECTION).Get<JwtOptions>();
        var key = new X509SecurityKey(_signingKeys.Certificate);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, session.UserId.ToString()),
            new("sid", session.Id.ToString()),
            new("token_use", "tenant"),
        };

        if (session.TenantId.HasValue)
        {
            claims.Add(new Claim("tid", session.TenantId.Value.ToString()));
            claims.Add(new Claim("mid", session.MembershipId!.Value.ToString()));
        }

        foreach (var pr in platformRoles)
            claims.Add(new Claim("prole", pr));

        foreach (var tr in tenantRoles)
            claims.Add(new Claim("trole", tr));

        var issuer = jwtOptions?.Issuer ?? "https://localhost:5251";
        var audience = jwtOptions?.Audience ?? "kyvo_api";

        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }
}
