using Kyvo.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Kyvo.Infrastructure.Configurations;

public sealed class PasswordPolicyOptionsValidator : IValidateOptions<PasswordPolicyOptions>
{
    public ValidateOptionsResult Validate(string? name, PasswordPolicyOptions options)
    {
        if (options.MinLength <= 0)
        {
            return ValidateOptionsResult.Fail(InfrastructureErrorMessages.PasswordPolicy.MIN_LENGTH_REQUIRED);
        }

        if (options.MinLength < 8)
        {
            return ValidateOptionsResult.Fail(InfrastructureErrorMessages.PasswordPolicy.MIN_LENGTH_MUST_BE_AT_LEAST_EIGHT);
        }

        return ValidateOptionsResult.Success;
    }
}
