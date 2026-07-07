using Kyvo.Domain.Enums;

namespace Kyvo.Application.Security;

public interface IIdentityProviderConfigCipher
{
    string? Encrypt(IdentityProviderType providerType, string? configJson);
    string? Decrypt(string? configJson);
}
