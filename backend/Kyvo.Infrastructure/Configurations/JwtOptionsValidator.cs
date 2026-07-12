using Kyvo.Application.Configurations;
using Kyvo.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Kyvo.Infrastructure.Configurations;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            errors.Add(InfrastructureErrorMessages.Jwt.ISSUER_REQUIRED);
        }
        else if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out _))
        {
            errors.Add(InfrastructureErrorMessages.Jwt.ISSUER_MUST_BE_ABSOLUTE_URI);
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            errors.Add(InfrastructureErrorMessages.Jwt.AUDIENCE_REQUIRED);
        }

        if (options.RefreshTokenDays <= 0)
        {
            errors.Add(
                options.RefreshTokenDays == 0
                    ? InfrastructureErrorMessages.Jwt.REFRESH_TOKEN_DAYS_REQUIRED
                    : InfrastructureErrorMessages.Jwt.REFRESH_TOKEN_DAYS_MUST_BE_POSITIVE);
        }

        var hasPath = !string.IsNullOrWhiteSpace(options.SigningKeyPath);
        var hasPem = !string.IsNullOrWhiteSpace(options.SigningKeyPem);
        var hasPemBase64 = !string.IsNullOrWhiteSpace(options.SigningKeyPemBase64);
        var sourceCount = (hasPath ? 1 : 0) + (hasPem ? 1 : 0) + (hasPemBase64 ? 1 : 0);
        // sourceCount == 0 → SigningCertificateProvider issues a development self-signed certificate.
        if (sourceCount > 1)
        {
            errors.Add(InfrastructureErrorMessages.Jwt.SIGNING_KEY_SOURCE_MUST_BE_EXCLUSIVE);
        }

        if (string.IsNullOrWhiteSpace(options.KeyId))
        {
            errors.Add(InfrastructureErrorMessages.Jwt.KEY_ID_REQUIRED);
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
