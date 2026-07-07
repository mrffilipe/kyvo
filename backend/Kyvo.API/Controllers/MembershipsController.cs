using Kyvo.API.Common;
using Kyvo.API.Models;
using Kyvo.Application.Common;
using Kyvo.Application.Queries.Memberships.ListMembershipsByTenant;
using Kyvo.Application.UseCases.Memberships.CreateMembership;
using Kyvo.Application.Services.UserScope;
using Kyvo.Application.UseCases.Memberships.RevokeMembership;
using Kyvo.Application.UseCases.Memberships.UpdateMembershipRoles;
using Microsoft.AspNetCore.Mvc;
using Kyvo.Application.Queries.Memberships.Dtos;

namespace Kyvo.API.Controllers;

/// <summary>
/// Tenant membership management (create, list, update roles, revoke).
/// </summary>
public sealed class MembershipsController : TenantV1ApiControllerBase
{
    private readonly ICreateMembershipUseCase _createMembershipUseCase;
    private readonly IListMembershipsByTenantQuery _listMembershipsByTenantQuery;
    private readonly IUpdateMembershipRolesUseCase _updateMembershipRolesUseCase;
    private readonly IRevokeMembershipUseCase _revokeMembershipUseCase;
    private readonly IUserScope _userScope;

    public MembershipsController(
        ICreateMembershipUseCase createMembershipUseCase,
        IListMembershipsByTenantQuery listMembershipsByTenantQuery,
        IUpdateMembershipRolesUseCase updateMembershipRolesUseCase,
        IRevokeMembershipUseCase revokeMembershipUseCase,
        IUserScope userScope)
    {
        _createMembershipUseCase = createMembershipUseCase;
        _listMembershipsByTenantQuery = listMembershipsByTenantQuery;
        _updateMembershipRolesUseCase = updateMembershipRolesUseCase;
        _revokeMembershipUseCase = revokeMembershipUseCase;
        _userScope = userScope;
    }

    [HttpPost("/v{version:apiVersion}/tenants/{tenantId:guid}/memberships")]
    [ProducesResponseType(typeof(CreatedIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatedIdResponse>> CreateMembership(Guid tenantId, [FromBody] CreateMembershipRequest request, CancellationToken ct)
    {
        var id = await _createMembershipUseCase.ExecuteAsync(
            request with
            {
                TenantId = tenantId,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return Ok(new CreatedIdResponse { Id = id });
    }

    [HttpGet("/v{version:apiVersion}/tenants/{tenantId:guid}/memberships")]
    [ProducesResponseType(typeof(PagedResult<MembershipDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MembershipDto>>> ListMembershipsByTenant(Guid tenantId, [FromQuery] ListMembershipsByTenantRequest request, CancellationToken ct)
    {
        var result = await _listMembershipsByTenantQuery.ExecuteAsync(
            request with
            {
                TenantId = tenantId,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMembershipRole(Guid id, [FromBody] UpdateMembershipRolesRequest request, CancellationToken ct)
    {
        await _updateMembershipRolesUseCase.ExecuteAsync(
            request with
            {
                MembershipId = id,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeMembership(Guid id, CancellationToken ct)
    {
        await _revokeMembershipUseCase.ExecuteAsync(
            new RevokeMembershipRequest
            {
                MembershipId = id,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return NoContent();
    }
}
