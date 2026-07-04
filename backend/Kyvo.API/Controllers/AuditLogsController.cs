using Kyvo.API.Common;
using Kyvo.Application.Common;
using Kyvo.Application.Queries.AuditLogs.ListAuditLogFilterOptions;
using Kyvo.Application.Queries.AuditLogs.ListAuditLogs;
using Kyvo.Application.Services.UserScope;
using Kyvo.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Kyvo.Application.Queries.AuditLogs.Dtos;

namespace Kyvo.API.Controllers;

/// <summary>
/// Tenant-scoped audit log queries (tenant owners and administrators).
/// </summary>
public sealed class AuditLogsController : V1ApiControllerBase
{
    private readonly IUserScope _userScope;
    private readonly IListAuditLogsQuery _listAuditLogsQuery;
    private readonly IListAuditLogFilterOptionsQuery _listAuditLogFilterOptionsQuery;

    public AuditLogsController(
        IUserScope userScope,
        IListAuditLogsQuery listAuditLogsQuery,
        IListAuditLogFilterOptionsQuery listAuditLogFilterOptionsQuery)
    {
        _userScope = userScope;
        _listAuditLogsQuery = listAuditLogsQuery;
        _listAuditLogFilterOptionsQuery = listAuditLogFilterOptionsQuery;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AuditLogItemDto>>> ListAuditLogs([FromQuery] ListAuditLogsRequest request, CancellationToken ct)
    {
        if (!_userScope.HasAnyTenantRole(TenantRoleDefaults.OWNER, TenantRoleDefaults.ADMIN))
        {
            return Forbid();
        }

        var result = await _listAuditLogsQuery.ExecuteAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("filter-options")]
    [ProducesResponseType(typeof(PagedResult<AuditLogFilterOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AuditLogFilterOptionDto>>> ListFilterOptions([FromQuery] ListAuditLogFilterOptionsRequest request, CancellationToken ct)
    {
        if (!_userScope.HasAnyTenantRole(TenantRoleDefaults.OWNER, TenantRoleDefaults.ADMIN))
        {
            return Forbid();
        }

        var result = await _listAuditLogFilterOptionsQuery.ExecuteAsync(request, ct);
        return Ok(result);
    }
}
