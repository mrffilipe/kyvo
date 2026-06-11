using Kyvo.Domain.Entities;

namespace Kyvo.Application.Services.Oidc;

public sealed class ApplicationClientValidationContext
{
    public required ApplicationClient Client { get; init; }
    public required IReadOnlyList<string> Scopes { get; init; }
}
