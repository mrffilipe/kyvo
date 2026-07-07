using Kyvo.API.Common;
using Kyvo.API.Models;
using Kyvo.Application.Common;
using Kyvo.Application.Queries.Invites.ListInvitesByTenant;
using Kyvo.Application.Queries.Tenants.CheckTenantKeyAvailability;
using Kyvo.Application.Queries.Tenants.GetTenantById;
using Kyvo.Application.Queries.Tenants.ListTenantsByUser;
using Kyvo.Application.UseCases.Tenants.CreateTenant;
using Kyvo.Application.Services.UserScope;
using Kyvo.Application.UseCases.Invites.AcceptInvite;
using Kyvo.Application.UseCases.Invites.InviteMember;
using Kyvo.Application.UseCases.Tenants.UpdateTenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kyvo.Application.Queries.Invites.Dtos;
using Kyvo.Application.Queries.Tenants.Dtos;

namespace Kyvo.API.Controllers;

/// <summary>
/// Tenant lifecycle, invites, and membership onboarding.
/// </summary>
public sealed class TenantsController : V1ApiControllerBase
{
    private readonly IUserScope _userScope;
    private readonly ICreateTenantUseCase _createTenantUseCase;
    private readonly IInviteMemberUseCase _inviteMemberUseCase;
    private readonly IListInvitesByTenantQuery _listInvitesByTenantQuery;
    private readonly IAcceptInviteUseCase _acceptInviteUseCase;
    private readonly IListTenantsByUserQuery _listTenantsByUserQuery;
    private readonly ICheckTenantKeyAvailabilityQuery _checkTenantKeyAvailabilityQuery;
    private readonly IGetTenantByIdQuery _getTenantByIdQuery;
    private readonly IUpdateTenantUseCase _updateTenantUseCase;

    public TenantsController(
        IUserScope userScope,
        ICreateTenantUseCase createTenantUseCase,
        IInviteMemberUseCase inviteMemberUseCase,
        IListInvitesByTenantQuery listInvitesByTenantQuery,
        IAcceptInviteUseCase acceptInviteUseCase,
        IListTenantsByUserQuery listTenantsByUserQuery,
        ICheckTenantKeyAvailabilityQuery checkTenantKeyAvailabilityQuery,
        IGetTenantByIdQuery getTenantByIdQuery,
        IUpdateTenantUseCase updateTenantUseCase)
    {
        _userScope = userScope;
        _createTenantUseCase = createTenantUseCase;
        _inviteMemberUseCase = inviteMemberUseCase;
        _listInvitesByTenantQuery = listInvitesByTenantQuery;
        _acceptInviteUseCase = acceptInviteUseCase;
        _listTenantsByUserQuery = listTenantsByUserQuery;
        _checkTenantKeyAvailabilityQuery = checkTenantKeyAvailabilityQuery;
        _getTenantByIdQuery = getTenantByIdQuery;
        _updateTenantUseCase = updateTenantUseCase;
    }

    /// <summary>
    /// Creates a new tenant (platform administrators only).
    /// </summary>
    [Authorize(Policy = "PlatformAdministrator")]
    [HttpPost]
    [ProducesResponseType(typeof(CreatedIdResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreatedIdResponse>> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        var id = await _createTenantUseCase.ExecuteAsync(
            request with
            {
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return CreatedAtAction(nameof(GetTenantById), new { id }, new CreatedIdResponse { Id = id });
    }

    /// <summary>
    /// Sends an invitation to join the tenant.
    /// </summary>
    [Authorize(Policy = "RequireTenantToken")]
    [HttpPost("{id:guid}/invites")]
    [ProducesResponseType(typeof(InviteMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InviteMemberResponse>> InviteMember(Guid id, [FromBody] InviteMemberRequest request, CancellationToken ct)
    {
        var result = await _inviteMemberUseCase.ExecuteAsync(
            request with
            {
                TenantId = id,
                InvitedByUserId = _userScope.UserId,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return Ok(new InviteMemberResponse { Id = result.Id, AcceptPath = result.AcceptPath });
    }

    /// <summary>
    /// Lists invites for a tenant.
    /// </summary>
    [Authorize(Policy = "RequireTenantToken")]
    [HttpGet("{id:guid}/invites")]
    [ProducesResponseType(typeof(PagedResult<TenantInviteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<TenantInviteDto>>> ListInvitesByTenant(Guid id, [FromQuery] ListInvitesByTenantRequest request, CancellationToken ct)
    {
        var result = await _listInvitesByTenantQuery.ExecuteAsync(
            request with
            {
                TenantId = id,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Accepts a tenant invite using the token from the invitation email.
    /// </summary>
    [HttpPost("~/api/v1/invites/accept")]
    [ProducesResponseType(typeof(CreatedMembershipIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatedMembershipIdResponse>> AcceptInvite([FromBody] AcceptInviteRequest request, CancellationToken ct)
    {
        var membershipId = await _acceptInviteUseCase.ExecuteAsync(
            request with { ActorUserId = _userScope.UserId },
            ct);

        return Ok(new CreatedMembershipIdResponse { MembershipId = membershipId });
    }

    /// <summary>
    /// Lists tenants the current user belongs to.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TenantDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TenantDto>>> ListTenantsByUser([FromQuery] ListTenantsByUserRequest request, CancellationToken ct)
    {
        var result = await _listTenantsByUserQuery.ExecuteAsync(
            request with
            {
                UserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Checks whether a tenant key is available.
    /// </summary>
    [HttpGet("keys/{key}/availability")]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AvailabilityDto>> CheckTenantKeyAvailability(string key, CancellationToken ct)
    {
        var result = await _checkTenantKeyAvailabilityQuery.ExecuteAsync(key, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a tenant by identifier when the caller has access.
    /// </summary>
    [Authorize(Policy = "RequireTenantToken")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantDto>> GetTenantById(Guid id, CancellationToken ct)
    {
        var result = await _getTenantByIdQuery.ExecuteAsync(
            new GetTenantByIdRequest
            {
                TenantId = id,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Updates tenant metadata (name).
    /// </summary>
    [Authorize(Policy = "RequireTenantToken")]
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken ct)
    {
        await _updateTenantUseCase.ExecuteAsync(
            request with
            {
                TenantId = id,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return NoContent();
    }
}
