namespace Kyvo.API.Tests.Fixtures;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiresDatabaseFactAttribute : Xunit.FactAttribute
{
    public override string Skip =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KYVO_TEST_DB"))
            ? "KYVO_TEST_DB is not set; integration tests require PostgreSQL."
            : null!;
}
