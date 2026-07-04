using Kyvo.Application.Common;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Queries.AuditLogs.ListAuditLogFilterOptions;
using Kyvo.Application.Services.UserScope;
using Kyvo.Domain.Exceptions;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Kyvo.Application.Queries.AuditLogs.Dtos;

namespace Kyvo.Infrastructure.Queries.AuditLogs;

public sealed class ListAuditLogFilterOptionsQuery : IListAuditLogFilterOptionsQuery
{
    private readonly ApplicationDbContext _context;
    private readonly IUserScope _userScope;

    public ListAuditLogFilterOptionsQuery(ApplicationDbContext context, IUserScope userScope)
    {
        _context = context;
        _userScope = userScope;
    }

    public async Task<PagedResult<AuditLogFilterOptionDto>> ExecuteAsync(ListAuditLogFilterOptionsRequest request, CancellationToken ct = default)
    {
        if (_userScope.TenantId is null)
        {
            throw new DomainValidationException(ApplicationErrorMessages.Auth.USER_HAS_NO_TENANT_ACCESS);
        }

        var field = request.Field.Trim().ToLowerInvariant();
        if (field is not "action" and not "resourcetype")
        {
            throw new DomainValidationException(ApplicationErrorMessages.OAuthClient.CONFIGURATION_INVALID);
        }

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var tenantId = _userScope.TenantId.Value;

        IQueryable<string> valuesQuery = field == "action"
            ? _context.AuditLogs.AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .Select(x => x.Action)
            : _context.AuditLogs.AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .Select(x => x.ResourceType);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            if (search.Length < 3)
            {
                throw new DomainValidationException(ApplicationErrorMessages.Search.QUERY_TOO_SHORT);
            }

            var pattern = $"%{search}%";
            valuesQuery = valuesQuery.Where(x => EF.Functions.ILike(x, pattern));
        }

        var distinctQuery = valuesQuery.Distinct().OrderBy(x => x);
        var total = await distinctQuery.CountAsync(ct);
        var values = await distinctQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AuditLogFilterOptionDto>
        {
            Items = values.Select(x => new AuditLogFilterOptionDto { Value = x }).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
