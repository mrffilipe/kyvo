using Kyvo.Application.Queries.Applications.Dtos;
using Kyvo.Application.Queries.Applications.GetApplicationBranding;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Queries.Applications;

public sealed class GetApplicationBrandingQuery : IGetApplicationBrandingQuery
{
    private readonly IApplicationRepository _applications;

    public GetApplicationBrandingQuery(IApplicationRepository applications) => _applications = applications;

    public async Task<ApplicationBrandingDto?> ExecuteAsync(Guid applicationId, CancellationToken ct = default)
    {
        var application = await _applications.GetByIdAsync(applicationId, ct);
        return application is null ? null : ApplicationDtoMapper.MapBrandingDto(application);
    }
}
