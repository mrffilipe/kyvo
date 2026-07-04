using System.Text.Json.Nodes;
using Kyvo.Application.Services.IdentityProvider;
using Kyvo.Application.Services.Security;
using Kyvo.Domain.Enums;

namespace Kyvo.Infrastructure.Services.IdentityProvider;

public sealed class IdentityProviderConfigCipher : IIdentityProviderConfigCipher
{
    // Every non-local provider type shares the same FederatedProviderConfig schema, so the sensitive
    // path is the same regardless of provider type: only ClientSecret is encrypted at rest.
    private const string ClientSecretPath = "ClientSecret";

    private readonly ISecretProtector _protector;

    public IdentityProviderConfigCipher(ISecretProtector protector) => _protector = protector;

    public string? Encrypt(IdentityProviderType providerType, string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson) || providerType == IdentityProviderType.Local)
        {
            return configJson;
        }

        if (JsonNode.Parse(configJson) is not JsonObject root)
        {
            return configJson;
        }

        EncryptPath(root, ClientSecretPath);
        return root.ToJsonString();
    }

    public string? Decrypt(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return configJson;
        }

        if (JsonNode.Parse(configJson) is not JsonObject root)
        {
            return configJson;
        }

        var mutated = false;
        foreach (var (key, value) in root.ToList())
        {
            if (value is not JsonValue jsonValue || !jsonValue.TryGetValue<string>(out var stringValue))
            {
                continue;
            }

            if (!_protector.IsProtected(stringValue))
            {
                continue;
            }

            var plaintextJson = _protector.Unprotect(stringValue);
            root[key] = JsonNode.Parse(plaintextJson);
            mutated = true;
        }

        return mutated ? root.ToJsonString() : configJson;
    }

    private void EncryptPath(JsonObject root, string path)
    {
        if (!root.TryGetPropertyValue(path, out var node) || node is null)
        {
            return;
        }

        if (node is JsonValue alreadyValue
            && alreadyValue.TryGetValue<string>(out var existing)
            && _protector.IsProtected(existing))
        {
            return;
        }

        var plaintextJson = node.ToJsonString();
        var encrypted = _protector.Protect(plaintextJson);
        root[path] = JsonValue.Create(encrypted);
    }
}
