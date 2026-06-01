using Kyvo.Application.Common;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.AuditLog;
using Kyvo.Application.Services.UserScope;
using Kyvo.Domain.Exceptions;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Services.AuditLog;

public sealed class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserScope _userScope;

    public AuditLogService(ApplicationDbContext context, IUserScope userScope)
    {
        _context = context;
        _userScope = userScope;
    }

    public async Task<PagedResult<AuditLogItemDto>> ListAsync(
        ListAuditLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var query = _context.AuditLogs.AsNoTracking();

        if (request.UserId.HasValue)
        {
            query = query.Where(x => x.UserId == request.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(x => x.Action == request.Action);
        }

        if (!string.IsNullOrWhiteSpace(request.ResourceType))
        {
            query = query.Where(x => x.ResourceType == request.ResourceType);
        }

        if (request.From.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= request.To.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                UserId = x.UserId,
                MembershipId = x.MembershipId,
                Action = x.Action,
                ResourceType = x.ResourceType,
                ResourceId = x.ResourceId,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<AuditLogFilterOptionDto>> ListFilterOptionsAsync(
        ListAuditLogFilterOptionsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_userScope.TenantId is null)
        {
            throw new DomainValidationException(ApplicationErrorMessages.Auth.UserHasNoTenantAccess);
        }

        var field = request.Field.Trim().ToLowerInvariant();
        if (field is not "action" and not "resourcetype")
        {
            throw new DomainValidationException(ApplicationErrorMessages.OAuthClient.ConfigurationInvalid);
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
                throw new DomainValidationException(ApplicationErrorMessages.Search.QueryTooShort);
            }

            var pattern = $"%{search}%";
            valuesQuery = valuesQuery.Where(x => EF.Functions.ILike(x, pattern));
        }

        var distinctQuery = valuesQuery.Distinct().OrderBy(x => x);
        var total = await distinctQuery.CountAsync(cancellationToken);
        var values = await distinctQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogFilterOptionDto>
        {
            Items = values.Select(x => new AuditLogFilterOptionDto { Value = x }).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
