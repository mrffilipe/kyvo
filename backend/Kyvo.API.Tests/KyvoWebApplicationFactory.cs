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
            var connectionString = Environment.GetEnvironmentVariable("KYVO_TEST_DB");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ConnectionString"] = connectionString
                });
            }
        });
    }
}
