using System.Security.Claims;

namespace Kyvo.Application.Services.Claims;

public interface IClaimsMappingService
{
    MappedExternalIdentity Map(ClaimsPrincipal externalPrincipal, string provider);
}

public sealed class MappedExternalIdentity
{
    public required string Provider { get; init; }
    public required string ProviderUserId { get; init; }
    public required string Email { get; init; }
    public bool EmailVerified { get; init; }
    public string? Name { get; init; }
    public string? GivenName { get; init; }
    public string? FamilyName { get; init; }
    public string? Picture { get; init; }
}
