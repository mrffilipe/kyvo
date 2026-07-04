namespace Kyvo.Infrastructure.Configurations;

public sealed record EmailOptions
{
    public const string SECTION = "Email";

    public required string FromAddress { get; init; }
    public required string Region { get; init; }
    public string? AccessKeyId { get; init; }
    public string? SecretAccessKey { get; init; }
    public string? SessionToken { get; init; }
}
