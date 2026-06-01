using System.Text.Json;
using Kyvo.Application.Exceptions;
using Kyvo.Application.IdentityProviderConfigs;
using Kyvo.Application.Services.IdentityProvider;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Infrastructure.Services.ExternalIdentityProvider;

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
            throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.ConfigRequired);
        }

        try
        {
            switch (providerType)
            {
                case IdentityProviderType.Firebase:
                    ValidateFirebase(configJson);
                    break;
                case IdentityProviderType.Cognito:
                    ValidateCognito(configJson);
                    break;
                case IdentityProviderType.Generic:
                    ValidateGeneric(configJson);
                    break;
                default:
                    throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.ConfigInvalid);
            }
        }
        catch (JsonException)
        {
            throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.ConfigInvalid);
        }
    }

    private static void ValidateFirebase(string configJson)
    {
        var config = JsonSerializer.Deserialize<FirebaseProviderConfig>(configJson, JsonOptions)
            ?? throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.ConfigInvalid);

        if (string.IsNullOrWhiteSpace(config.ProjectId)
            || string.IsNullOrWhiteSpace(config.WebApiKey)
            || config.ServiceAccount.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            || config.ServiceAccount.ValueKind != JsonValueKind.Object)
        {
            throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.ConfigInvalid);
        }

        if (!config.ServiceAccount.TryGetProperty("type", out var typeElement)
            || typeElement.GetString() != "service_account")
        {
            throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.ConfigInvalid);
        }

        if (config.ServiceAccount.TryGetProperty("project_id", out var projectIdElement))
        {
            var serviceAccountProjectId = projectIdElement.GetString();
            if (!string.IsNullOrWhiteSpace(serviceAccountProjectId)
                && !string.Equals(
                    serviceAccountProjectId.Trim(),
                    config.ProjectId.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.ConfigInvalid);
            }
        }
    }

    private static void ValidateCognito(string configJson)
    {
        var config = JsonSerializer.Deserialize<CognitoProviderConfig>(configJson, JsonOptions)
            ?? throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.ConfigInvalid);

        if (string.IsNullOrWhiteSpace(config.UserPoolId)
            || string.IsNullOrWhiteSpace(config.Region)
            || string.IsNullOrWhiteSpace(config.ClientId))
        {
            throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.ConfigInvalid);
        }
    }

    private static void ValidateGeneric(string configJson)
    {
        var config = JsonSerializer.Deserialize<GenericProviderConfig>(configJson, JsonOptions)
            ?? throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.ConfigInvalid);

        if (string.IsNullOrWhiteSpace(config.Issuer)
            || string.IsNullOrWhiteSpace(config.JwksUri)
            || string.IsNullOrWhiteSpace(config.Audience))
        {
            throw new DomainValidationException(ApplicationErrorMessages.IdentityProvider.ConfigInvalid);
        }
    }
}
