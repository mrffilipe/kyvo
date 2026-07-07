using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Kyvo.Application.Configurations;

namespace Kyvo.Infrastructure.Services.Certificates;

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
        else if (!string.IsNullOrWhiteSpace(options.SigningKeyPath))
        {
            pem = File.ReadAllText(options.SigningKeyPath!);
        }
        else
        {
            throw new InvalidOperationException("No RSA key found in JwtOptions.");
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return rsa;
    }
}
