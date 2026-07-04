using Kyvo.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Kyvo.Infrastructure.Configurations;

public sealed class RedisOptionsValidator : IValidateOptions<RedisOptions>
{
    public ValidateOptionsResult Validate(string? name, RedisOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.InstanceName))
        {
            errors.Add(
                !string.IsNullOrWhiteSpace(options.ConnectionString)
                    ? InfrastructureErrorMessages.Redis.INSTANCE_NAME_REQUIRED_WHEN_CONNECTION_STRING_SET
                    : InfrastructureErrorMessages.Redis.INSTANCE_NAME_REQUIRED);
        }

        if (options.TenantIdentifierCacheMinutes <= 0)
        {
            errors.Add(
                options.TenantIdentifierCacheMinutes == 0
                    ? InfrastructureErrorMessages.Redis.TENANT_IDENTIFIER_CACHE_MINUTES_REQUIRED
                    : InfrastructureErrorMessages.Redis.TENANT_IDENTIFIER_CACHE_MINUTES_MUST_BE_POSITIVE);
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
