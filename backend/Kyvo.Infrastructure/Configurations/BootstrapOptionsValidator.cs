using System.Net.Mail;
using Kyvo.Application.Configurations;
using Kyvo.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Kyvo.Infrastructure.Configurations;

/// <summary>
/// Validates <see cref="BootstrapOptions"/>: bootstrap is optional, but when any field is
/// provided the email and password must both be present and the email must be syntactically valid.
/// </summary>
public sealed class BootstrapOptionsValidator : IValidateOptions<BootstrapOptions>
{
    public ValidateOptionsResult Validate(string? name, BootstrapOptions options)
    {
        var anyProvided =
            !string.IsNullOrWhiteSpace(options.AdminEmail) ||
            !string.IsNullOrWhiteSpace(options.AdminPassword) ||
            !string.IsNullOrWhiteSpace(options.AdminDisplayName);

        if (!anyProvided)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(options.AdminEmail))
        {
            errors.Add(InfrastructureErrorMessages.Bootstrap.ADMIN_EMAIL_REQUIRED_WHEN_ANY_PROVIDED);
        }
        else if (!MailAddress.TryCreate(options.AdminEmail, out _))
        {
            errors.Add(InfrastructureErrorMessages.Bootstrap.ADMIN_EMAIL_INVALID);
        }

        if (string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            errors.Add(InfrastructureErrorMessages.Bootstrap.ADMIN_PASSWORD_REQUIRED_WHEN_ANY_PROVIDED);
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
