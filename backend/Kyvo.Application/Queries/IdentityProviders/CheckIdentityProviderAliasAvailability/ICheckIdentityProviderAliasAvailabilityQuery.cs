using Kyvo.Application.Common;

namespace Kyvo.Application.Queries.IdentityProviders.CheckIdentityProviderAliasAvailability;

public interface ICheckIdentityProviderAliasAvailabilityQuery
{
    Task<AvailabilityDto> ExecuteAsync(string alias, CancellationToken ct = default);
}
