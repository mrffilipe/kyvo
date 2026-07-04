using Kyvo.Application.Queries.Applications.Dtos;

namespace Kyvo.Application.Queries.Applications.GetApplicationById;

public interface IGetApplicationByIdQuery
{
    Task<ApplicationDto?> ExecuteAsync(GetApplicationByIdRequest request, CancellationToken ct = default);
}
