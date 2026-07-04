using Kyvo.Application.Common;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Queries.Users.SearchUsers;
using Kyvo.Domain.Exceptions;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Kyvo.Application.Queries.Users.Dtos;

namespace Kyvo.Infrastructure.Queries.Users;

public sealed class SearchUsersQuery : ISearchUsersQuery
{
    private readonly ApplicationDbContext _context;

    public SearchUsersQuery(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<UserPickerDto>> ExecuteAsync(SearchUsersRequest request, CancellationToken ct = default)
    {
        var search = request.Search.Trim();
        if (search.Length < 3)
        {
            throw new DomainValidationException(ApplicationErrorMessages.Search.QUERY_TOO_SHORT);
        }

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var pattern = $"%{search}%";

        var query = _context.Users
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x =>
                EF.Functions.ILike(x.Email!, pattern)
                || EF.Functions.ILike(x.DisplayName, pattern));

        var total = await query.CountAsync(ct);
        var users = await query
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = users
            .Select(x => new UserPickerDto
            {
                Id = x.Id,
                Email = x.Email!,
                DisplayName = x.DisplayName,
                PhotoUrl = x.PhotoUrl
            })
            .ToList();

        return new PagedResult<UserPickerDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
