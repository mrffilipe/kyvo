using System.Text.Json;
using System.Text.Json.Nodes;
using Kyvo.Application.Security;
using Kyvo.Domain.Enums;
using Microsoft.AspNetCore.DataProtection;

namespace Kyvo.Infrastructure.Security;

public class IdentityProviderConfigCipher : IIdentityProviderConfigCipher
{
    private const string Prefix = "enc:v1:";
    private const string ClientSecretPath = "ClientSecret";
    private readonly IDataProtector _protector;

    public IdentityProviderConfigCipher(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Kyvo.IdpConfig");
    }

    public string? Encrypt(IdentityProviderType providerType, string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson) || providerType == IdentityProviderType.Local)
            return configJson;

        if (JsonNode.Parse(configJson) is not JsonObject root)
            return configJson;

        if (root.TryGetPropertyValue(ClientSecretPath, out var node) && node is JsonValue value)
        {
            if (value.TryGetValue<string>(out var str) && !str.StartsWith(Prefix))
            {
                var encrypted = Prefix + _protector.Protect(str);
                root[ClientSecretPath] = JsonValue.Create(encrypted);
            }
        }

        return root.ToJsonString();
    }

    public string? Decrypt(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
            return configJson;

        if (JsonNode.Parse(configJson) is not JsonObject root)
            return configJson;

        var mutated = false;
        if (root.TryGetPropertyValue(ClientSecretPath, out var node) && node is JsonValue value)
        {
            if (value.TryGetValue<string>(out var str) && str.StartsWith(Prefix))
            {
                var decrypted = _protector.Unprotect(str.Substring(Prefix.Length));
                root[ClientSecretPath] = JsonValue.Create(decrypted);
                mutated = true;
            }
        }

        return mutated ? root.ToJsonString() : configJson;
    }
}
