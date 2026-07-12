using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace Kyvo.IDP.Infrastructure.Services.Certificates;

/// <summary>
/// Development self-signed signing/encryption certificate for OpenIddict.
/// Production: load a real certificate from Key Vault / PEM / path (see README).
/// </summary>
public sealed class SigningCertificateProvider
{
    public X509Certificate2 Certificate { get; }

    public SigningCertificateProvider(IConfiguration configuration)
    {
        var path = configuration["Oidc:SigningCertificatePath"];
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            var password = configuration["Oidc:SigningCertificatePassword"];
#pragma warning disable SYSLIB0057 // Prefer X509CertificateLoader on .NET 9+
            Certificate = string.IsNullOrEmpty(password)
                ? new X509Certificate2(path)
                : new X509Certificate2(path, password);
#pragma warning restore SYSLIB0057
            return;
        }

        Certificate = CreateDevelopmentCertificate();
    }

    private static X509Certificate2 CreateDevelopmentCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Kyvo-IDP-Dev-Signing",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));
    }
}
