using Kyvo.Application.Queries.Applications.GetApplicationById;
using Kyvo.Domain.Repositories;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Kyvo.Application.Queries.Applications.Dtos;

namespace Kyvo.Infrastructure.Queries.Applications;

public sealed class GetApplicationByIdQuery : IGetApplicationByIdQuery
{
    private readonly IApplicationClientRepository _clients;
    private readonly ApplicationDbContext _context;

    public GetApplicationByIdQuery(IApplicationClientRepository clients, ApplicationDbContext context)
    {
        _clients = clients;
        _context = context;
    }

    public async Task<ApplicationDto?> ExecuteAsync(GetApplicationByIdRequest request, CancellationToken ct = default)
    {
        var application = await _context.Applications
            .AsNoTracking()
            .Where(x => x.Id == request.ApplicationId)
            .Select(ApplicationDtoMapper.MapToDtoExpression)
            .FirstOrDefaultAsync(ct);

        if (application is null)
        {
            return null;
        }

        var clients = await _clients.ListByApplicationIdAsync(request.ApplicationId, ct);
        return application with
        {
            Clients = clients.Select(ApplicationDtoMapper.MapClientSummary).ToList()
        };
    }
}
