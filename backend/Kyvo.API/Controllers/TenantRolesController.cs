using Kyvo.API.Common;
using Kyvo.API.Models;
using Kyvo.Application.Common;
using Kyvo.Application.Queries.TenantRoles.Dtos;
using Kyvo.Application.Queries.TenantRoles.ListTenantRoles;
using Kyvo.Application.Services.UserScope;
using Kyvo.Application.UseCases.TenantRoles.CreateTenantRole;
using Kyvo.Application.UseCases.TenantRoles.DeleteTenantRole;
using Kyvo.Application.UseCases.TenantRoles.UpdateTenantRole;
using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Controllers;

/// <summary>
/// Custom tenant role definitions (RBAC keys within a tenant).
/// </summary>
public sealed class TenantRolesController : TenantV1ApiControllerBase
{
    private readonly ICreateTenantRoleUseCase _createTenantRoleUseCase;
    private readonly IListTenantRolesQuery _listTenantRolesQuery;
    private readonly IUpdateTenantRoleUseCase _updateTenantRoleUseCase;
    private readonly IDeleteTenantRoleUseCase _deleteTenantRoleUseCase;
    private readonly IUserScope _userScope;

    public TenantRolesController(
        ICreateTenantRoleUseCase createTenantRoleUseCase,
        IListTenantRolesQuery listTenantRolesQuery,
        IUpdateTenantRoleUseCase updateTenantRoleUseCase,
        IDeleteTenantRoleUseCase deleteTenantRoleUseCase,
        IUserScope userScope)
    {
        _createTenantRoleUseCase = createTenantRoleUseCase;
        _listTenantRolesQuery = listTenantRolesQuery;
        _updateTenantRoleUseCase = updateTenantRoleUseCase;
        _deleteTenantRoleUseCase = deleteTenantRoleUseCase;
        _userScope = userScope;
    }

    [HttpPost("/v{version:apiVersion}/tenants/{tenantId:guid}/roles")]
    [ProducesResponseType(typeof(CreatedIdResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreatedIdResponse>> CreateTenantRole(Guid tenantId, [FromBody] CreateTenantRoleRequest request, CancellationToken ct)
    {
        var id = await _createTenantRoleUseCase.ExecuteAsync(
            request with
            {
                TenantId = tenantId,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return Ok(new CreatedIdResponse { Id = id });
    }

    [HttpGet("/v{version:apiVersion}/tenants/{tenantId:guid}/roles")]
    [ProducesResponseType(typeof(PagedResult<TenantRoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TenantRoleDto>>> ListTenantRoles(Guid tenantId, [FromQuery] ListTenantRolesRequest request, CancellationToken ct)
    {
        var result = await _listTenantRolesQuery.ExecuteAsync(
            request with { TenantId = tenantId },
            ct);

        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTenantRole(Guid id, [FromBody] UpdateTenantRoleRequest request, CancellationToken ct)
    {
        await _updateTenantRoleUseCase.ExecuteAsync(
            request with
            {
                RoleId = id,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTenantRole(Guid id, CancellationToken ct)
    {
        await _deleteTenantRoleUseCase.ExecuteAsync(
            new DeleteTenantRoleRequest
            {
                RoleId = id,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return NoContent();
    }
}
