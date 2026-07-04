using Kyvo.Application.Common;
using Kyvo.Application.Queries.Applications.CheckApplicationSlugAvailability;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Queries.Applications;

public sealed class CheckApplicationSlugAvailabilityQuery : ICheckApplicationSlugAvailabilityQuery
{
    private readonly IApplicationRepository _applications;

    public CheckApplicationSlugAvailabilityQuery(IApplicationRepository applications) => _applications = applications;

    public async Task<AvailabilityDto> ExecuteAsync(string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return new AvailabilityDto { Available = false };
        }

        var normalized = slug.Trim().ToLowerInvariant();
        var exists = await _applications.SlugAlreadyExistsAsync(normalized, ct);
        return new AvailabilityDto { Available = !exists };
    }
}
