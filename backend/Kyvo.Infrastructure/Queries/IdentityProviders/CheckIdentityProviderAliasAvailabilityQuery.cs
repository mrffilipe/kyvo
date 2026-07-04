using Kyvo.Application.Common;
using Kyvo.Application.Queries.IdentityProviders.CheckIdentityProviderAliasAvailability;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Queries.IdentityProviders;

public sealed class CheckIdentityProviderAliasAvailabilityQuery : ICheckIdentityProviderAliasAvailabilityQuery
{
    private readonly IIdentityProviderRepository _identityProviders;

    public CheckIdentityProviderAliasAvailabilityQuery(IIdentityProviderRepository identityProviders) => _identityProviders = identityProviders;

    public async Task<AvailabilityDto> ExecuteAsync(string alias, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return new AvailabilityDto { Available = false };
        }

        var normalized = alias.Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[a-z0-9_-]+$"))
        {
            return new AvailabilityDto { Available = false };
        }

        var exists = await _identityProviders.AliasAlreadyExistsAsync(normalized, ct);
        return new AvailabilityDto { Available = !exists };
    }
}
