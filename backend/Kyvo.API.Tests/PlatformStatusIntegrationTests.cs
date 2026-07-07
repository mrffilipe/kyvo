using Xunit;

namespace Kyvo.API.Tests;

public sealed class PlatformStatusIntegrationTests
{
    private static bool HasTestDatabase =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KYVO_TEST_DB"));

    [Fact]
    public async Task GetPlatformStatus_ReturnsOk_WhenDatabaseConfigured()
    {
        if (!HasTestDatabase)
        {
            return;
        }

        await using var factory = new KyvoWebApplicationFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/platform/status");
        response.EnsureSuccessStatusCode();
    }
}
