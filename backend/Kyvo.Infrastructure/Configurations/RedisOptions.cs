namespace Kyvo.Infrastructure.Configurations;

public sealed record RedisOptions
{
    public const string SECTION = "Redis";

    public required string ConnectionString { get; init; }
    public required string InstanceName { get; init; }
    public required int TenantIdentifierCacheMinutes { get; init; }
}
