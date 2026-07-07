using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Kyvo.Application.Configurations;
using Microsoft.Extensions.Configuration;

namespace Kyvo.Infrastructure.Services.Certificates;

/// <summary>
/// Shared signing certificate for OpenIddict Server and tenant access tokens (same JWKS).
/// </summary>
public sealed class SigningCertificateProvider
{
    public X509Certificate2 Certificate { get; }

    public SigningCertificateProvider(IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SECTION).Get<JwtOptions>();
        if (jwtOptions != null
            && (!string.IsNullOrEmpty(jwtOptions.SigningKeyPemBase64)
                || !string.IsNullOrEmpty(jwtOptions.SigningKeyPem)
                || !string.IsNullOrEmpty(jwtOptions.SigningKeyPath)))
        {
            Certificate = SigningCertificateFactory.Create(jwtOptions);
            return;
        }

        Certificate = CreateDevelopmentCertificate();
    }

    private static X509Certificate2 CreateDevelopmentCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Kyvo-Dev-Signing",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));
    }
}
