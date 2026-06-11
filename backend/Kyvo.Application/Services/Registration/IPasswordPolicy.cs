namespace Kyvo.Application.Services.Registration;

/// <summary>
/// Contract for password policy enforcement. Implementations are configured through
/// <c>PasswordPolicyOptions</c> in Infrastructure and may evolve independently of the application code.
/// </summary>
public interface IPasswordPolicy
{
    PasswordPolicyResult Validate(string password);
}
