using Kyvo.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Kyvo.Infrastructure.Configurations;

public sealed class InviteOptionsValidator : IValidateOptions<InviteOptions>
{
    public ValidateOptionsResult Validate(string? name, InviteOptions options)
    {
        if (options.ExpirationHours <= 0)
        {
            return ValidateOptionsResult.Fail(
                options.ExpirationHours == 0
                    ? InfrastructureErrorMessages.Invite.EXPIRATION_HOURS_REQUIRED
                    : InfrastructureErrorMessages.Invite.EXPIRATION_HOURS_MUST_BE_POSITIVE);
        }

        return ValidateOptionsResult.Success;
    }
}
