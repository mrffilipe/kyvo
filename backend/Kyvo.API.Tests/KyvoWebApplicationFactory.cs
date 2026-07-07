using Kyvo.API.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Kyvo.API.Tests;

public sealed class KyvoWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = TestSigningKey.Issuer,
                ["Jwt:Audience"] = TestSigningKey.Audience,
                ["Jwt:SigningKeyPem"] = TestSigningKey.Pem,
                ["Jwt:SigningKeyPath"] = string.Empty,
                ["Jwt:SigningKeyPemBase64"] = string.Empty,
                ["Jwt:KeyId"] = "integration-test",
                ["Redis:ConnectionString"] = string.Empty,
            };

            var connectionString = Environment.GetEnvironmentVariable("KYVO_TEST_DB");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                overrides["Database:ConnectionString"] = connectionString;
            }

            config.AddInMemoryCollection(overrides);
        });
    }
}
