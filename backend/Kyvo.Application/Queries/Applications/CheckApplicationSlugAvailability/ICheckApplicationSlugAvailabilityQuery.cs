using Kyvo.Application.Common;

namespace Kyvo.Application.Queries.Applications.CheckApplicationSlugAvailability;

public interface ICheckApplicationSlugAvailabilityQuery
{
    Task<AvailabilityDto> ExecuteAsync(string slug, CancellationToken ct = default);
}
