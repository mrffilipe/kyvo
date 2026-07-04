using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Kyvo.Application.Configurations;

namespace Kyvo.Infrastructure.Services.Certificates;

/// <summary>
/// Wraps the RSA key configured under <c>Jwt:SigningKey*</c> into an ephemeral self-signed
/// <see cref="X509Certificate2"/>, reused by OpenIddict for both signing and encryption. This keeps the
/// existing <c>Jwt</c> configuration surface (PEM file/inline/base64) unchanged from the previous
/// hand-rolled JWT signing service.
/// </summary>
public static class SigningCertificateFactory
{
    public static X509Certificate2 Create(JwtOptions options)
    {
        var rsa = LoadRsaKey(options);
        var request = new CertificateRequest(
            $"CN={options.KeyId}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5));
    }

    private static RSA LoadRsaKey(JwtOptions options)
    {
        string pem;
        if (!string.IsNullOrWhiteSpace(options.SigningKeyPemBase64))
        {
            var normalized = string.Concat(options.SigningKeyPemBase64.Where(c => !char.IsWhiteSpace(c)));
            pem = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
        }
        else if (!string.IsNullOrWhiteSpace(options.SigningKeyPem))
        {
            pem = options.SigningKeyPem;
        }
        else
        {
            pem = File.ReadAllText(options.SigningKeyPath!);
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return rsa;
    }
}
