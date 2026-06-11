using Kyvo.API.Common;
using Kyvo.API.Models;
using Kyvo.Application.Common;
using Kyvo.Application.Services.Membership;
using Kyvo.Application.Services.UserScope;
using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Controllers;

/// <summary>
/// Tenant membership management (create, list, update roles, revoke).
/// </summary>
public sealed class MembershipsController : V1ApiControllerBase
{
    private readonly IMembershipService _membershipService;
    private readonly IUserScope _userScope;

    public MembershipsController(IMembershipService membershipService, IUserScope userScope)
    {
        _membershipService = membershipService;
        _userScope = userScope;
    }

    /// <summary>
    /// Adds a user to a tenant with the given roles.
    /// </summary>
    [HttpPost("/v{version:apiVersion}/tenants/{tenantId:guid}/memberships")]
    [ProducesResponseType(typeof(CreatedIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatedIdResponse>> CreateMembership(
        Guid tenantId,
        [FromBody] CreateMembershipRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _membershipService.CreateMembershipAsync(
            request with
            {
                TenantId = tenantId,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            cancellationToken);

        return Ok(new CreatedIdResponse(id));
    }

    /// <summary>
    /// Lists memberships for a tenant.
    /// </summary>
    [HttpGet("/v{version:apiVersion}/tenants/{tenantId:guid}/memberships")]
    [ProducesResponseType(typeof(PagedResult<MembershipDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MembershipDto>>> ListMembershipsByTenant(
        Guid tenantId,
        [FromQuery] ListMembershipsByTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _membershipService.ListByTenantAsync(
            request with
            {
                TenantId = tenantId,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Updates the roles assigned to a membership.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMembershipRole(
        Guid id,
        [FromBody] UpdateMembershipRolesRequest request,
        CancellationToken cancellationToken)
    {
        await _membershipService.UpdateRolesAsync(
            request with
            {
                MembershipId = id,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Revokes a membership (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeMembership(Guid id, CancellationToken cancellationToken)
    {
        await _membershipService.RevokeMembershipAsync(
            new RevokeMembershipRequest
            {
                MembershipId = id,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            cancellationToken);

        return NoContent();
    }
}
