using System.Security.Cryptography;

namespace Kyvo.API.Tests.Fixtures;

/// <summary>
/// Ephemeral RSA key shared by the test host and <see cref="TestAccessTokenFactory"/> within a test run.
/// </summary>
internal static class TestSigningKey
{
    private static readonly Lazy<string> LazyPem = new(() =>
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportPkcs8PrivateKeyPem();
    });

    public const string Issuer = "https://localhost:5001";
    public const string Audience = "kyvo-api";

    public static string Pem => LazyPem.Value;
}
