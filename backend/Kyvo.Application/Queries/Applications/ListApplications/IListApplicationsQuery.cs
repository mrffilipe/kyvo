using Kyvo.Application.Common;
using Kyvo.Application.Queries.Applications.Dtos;

namespace Kyvo.Application.Queries.Applications.ListApplications;

public interface IListApplicationsQuery
{
    Task<PagedResult<ApplicationDto>> ExecuteAsync(ListApplicationsRequest request, CancellationToken ct = default);
}
