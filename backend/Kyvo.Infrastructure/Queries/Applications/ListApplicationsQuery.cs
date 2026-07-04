using Kyvo.Application.Common;
using Kyvo.Application.Queries.Applications.ListApplications;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Kyvo.Application.Queries.Applications.Dtos;

namespace Kyvo.Infrastructure.Queries.Applications;

public sealed class ListApplicationsQuery : IListApplicationsQuery
{
    private readonly ApplicationDbContext _context;

    public ListApplicationsQuery(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<ApplicationDto>> ExecuteAsync(ListApplicationsRequest request, CancellationToken ct = default)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var query = _context.Applications.AsNoTracking();
        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ApplicationDtoMapper.MapToDtoExpression)
            .ToListAsync(ct);

        return new PagedResult<ApplicationDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
