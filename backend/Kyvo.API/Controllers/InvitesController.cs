using Kyvo.API.Common;
using Kyvo.Application.Services.Tenant;
using Kyvo.Application.Services.UserScope;
using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Controllers;

/// <summary>
/// Tenant invite lifecycle operations.
/// </summary>
public sealed class InvitesController : V1ApiControllerBase
{
    private readonly IUserScope _userScope;
    private readonly ITenantService _tenantService;

    public InvitesController(IUserScope userScope, ITenantService tenantService)
    {
        _userScope = userScope;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Revokes a pending tenant invite.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeInvite(Guid id, CancellationToken cancellationToken)
    {
        await _tenantService.RevokeInviteAsync(
            new RevokeInviteRequest
            {
                InviteId = id,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            cancellationToken);

        return NoContent();
    }
}
