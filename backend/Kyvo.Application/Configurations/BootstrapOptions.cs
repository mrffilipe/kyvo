namespace Kyvo.Application.Configurations;

public sealed record BootstrapOptions
{
    public const string SECTION = "Bootstrap";

    public string? AdminEmail { get; init; }
    public string? AdminPassword { get; init; }
    public string? AdminDisplayName { get; init; }
}
