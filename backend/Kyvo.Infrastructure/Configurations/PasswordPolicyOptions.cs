namespace Kyvo.Infrastructure.Configurations;

/// <summary>
/// Tunable password policy enforced by <c>PasswordPolicy</c> on self-registration and password updates.
/// </summary>
public sealed record PasswordPolicyOptions
{
    public const string SECTION = "PasswordPolicy";

    public required int MinLength { get; init; }
    public required bool RequireDigit { get; init; }
    public required bool RequireLetter { get; init; }
}
