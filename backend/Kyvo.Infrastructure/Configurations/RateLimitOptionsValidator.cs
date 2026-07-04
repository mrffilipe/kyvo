using Kyvo.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Kyvo.Infrastructure.Configurations;

public sealed class RateLimitOptionsValidator : IValidateOptions<RateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitOptions options)
    {
        var errors = new List<string>();
        if (options.AccountRegisterPermitLimit <= 0)
        {
            errors.Add(
                options.AccountRegisterPermitLimit == 0
                    ? InfrastructureErrorMessages.RateLimit.ACCOUNT_REGISTER_PERMIT_LIMIT_REQUIRED
                    : InfrastructureErrorMessages.RateLimit.ACCOUNT_REGISTER_PERMIT_LIMIT_MUST_BE_POSITIVE);
        }

        if (options.AccountRegisterWindowMinutes <= 0)
        {
            errors.Add(
                options.AccountRegisterWindowMinutes == 0
                    ? InfrastructureErrorMessages.RateLimit.ACCOUNT_REGISTER_WINDOW_MINUTES_REQUIRED
                    : InfrastructureErrorMessages.RateLimit.ACCOUNT_REGISTER_WINDOW_MINUTES_MUST_BE_POSITIVE);
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
