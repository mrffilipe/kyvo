using System.Net.Mail;
using Kyvo.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Kyvo.Infrastructure.Configurations;

public sealed class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            errors.Add(InfrastructureErrorMessages.Email.FROM_ADDRESS_REQUIRED);
        }
        else if (!MailAddress.TryCreate(options.FromAddress, out _))
        {
            errors.Add(InfrastructureErrorMessages.Email.FROM_ADDRESS_INVALID);
        }

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            errors.Add(InfrastructureErrorMessages.Email.REGION_REQUIRED);
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
