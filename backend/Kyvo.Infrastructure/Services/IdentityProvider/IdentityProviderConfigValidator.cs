using System.Text.Json;
using Kyvo.Application.Exceptions;
using Kyvo.Application.IdentityProviderConfigs;
using Kyvo.Application.Services.IdentityProvider;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Infrastructure.Services.IdentityProvider;

public sealed class IdentityProviderConfigValidator : IIdentityProviderConfigValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public void ValidateForSave(IdentityProviderType providerType, string? configJson)
    {
        if (providerType == IdentityProviderType.Local)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(configJson))
        {
            throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.CONFIG_REQUIRED);
        }

        FederatedProviderConfig config;
        try
        {
            config = JsonSerializer.Deserialize<FederatedProviderConfig>(configJson, JsonOptions)
                ?? throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.CONFIG_INVALID);
        }
        catch (JsonException)
        {
            throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.CONFIG_INVALID);
        }

        if (string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.ClientSecret))
        {
            throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.CONFIG_INVALID);
        }

        if (providerType == IdentityProviderType.GenericOidc && string.IsNullOrWhiteSpace(config.Issuer))
        {
            throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.CONFIG_INVALID);
        }
    }
}
