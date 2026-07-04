using Kyvo.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Kyvo.Infrastructure.Configurations;

public sealed class SecretProtectionOptionsValidator : IValidateOptions<SecretProtectionOptions>
{
    public ValidateOptionsResult Validate(string? name, SecretProtectionOptions options)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(options.KeyDirectoryPath))
        {
            errors.Add(InfrastructureErrorMessages.SecretProtection.KEY_DIRECTORY_PATH_REQUIRED);
        }

        if (string.IsNullOrWhiteSpace(options.ApplicationName))
        {
            errors.Add(InfrastructureErrorMessages.SecretProtection.APPLICATION_NAME_REQUIRED);
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
