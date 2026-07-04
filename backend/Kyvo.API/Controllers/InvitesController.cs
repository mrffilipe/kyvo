using Kyvo.API.Common;
using Kyvo.Application.UseCases.Invites.RevokeInvite;
using Microsoft.AspNetCore.Mvc;
using Kyvo.Application.Services.UserScope;

namespace Kyvo.API.Controllers;

/// <summary>
/// Tenant invite lifecycle operations.
/// </summary>
public sealed class InvitesController : V1ApiControllerBase
{
    private readonly IUserScope _userScope;
    private readonly IRevokeInviteUseCase _revokeInviteUseCase;

    public InvitesController(IUserScope userScope, IRevokeInviteUseCase revokeInviteUseCase)
    {
        _userScope = userScope;
        _revokeInviteUseCase = revokeInviteUseCase;
    }

    /// <summary>
    /// Revokes a pending tenant invite.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeInvite(Guid id, CancellationToken ct)
    {
        await _revokeInviteUseCase.ExecuteAsync(
            new RevokeInviteRequest
            {
                InviteId = id,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return NoContent();
    }
}
