namespace Kyvo.Application.Services.Registration;

public sealed record PasswordPolicyResult
{
    public required bool IsValid { get; init; }

    public required IReadOnlyList<string> Errors { get; init; }

    public static PasswordPolicyResult Success { get; } = new() { IsValid = true, Errors = Array.Empty<string>() };

    public static PasswordPolicyResult Failed(IReadOnlyList<string> errors) =>
        new() { IsValid = false, Errors = errors };
}
