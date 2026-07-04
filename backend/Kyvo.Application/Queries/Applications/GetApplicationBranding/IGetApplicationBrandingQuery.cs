using Kyvo.Application.Queries.Applications.Dtos;

namespace Kyvo.Application.Queries.Applications.GetApplicationBranding;

public interface IGetApplicationBrandingQuery
{
    Task<ApplicationBrandingDto?> ExecuteAsync(Guid applicationId, CancellationToken ct = default);
}
