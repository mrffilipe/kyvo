using Kyvo.API.Common;
using Kyvo.API.Models;
using Kyvo.Application.Queries.Auth.Dtos;
using Kyvo.Application.Queries.Auth.ListActiveSessions;
using Kyvo.Application.Services.UserScope;
using Kyvo.Application.UseCases.Auth;
using Kyvo.Application.UseCases.Auth.DeleteAccount;
using Kyvo.Application.UseCases.Auth.RevokeSession;
using Kyvo.Application.UseCases.Auth.SubscribeTenant;
using Kyvo.Application.UseCases.Auth.SwitchTenant;
using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Controllers;

/// <summary>
/// Authenticated tenant context: subscribe, switch tenant, and session management.
/// </summary>
public sealed class AuthController : V1ApiControllerBase
{
    private readonly ISubscribeTenantUseCase _subscribeTenantUseCase;
    private readonly ISwitchTenantUseCase _switchTenantUseCase;
    private readonly IListActiveSessionsQuery _listActiveSessionsQuery;
    private readonly IRevokeSessionUseCase _revokeSessionUseCase;
    private readonly IDeleteAccountUseCase _deleteAccountUseCase;
    private readonly IUserScope _userScope;

    public AuthController(
        ISubscribeTenantUseCase subscribeTenantUseCase,
        ISwitchTenantUseCase switchTenantUseCase,
        IListActiveSessionsQuery listActiveSessionsQuery,
        IRevokeSessionUseCase revokeSessionUseCase,
        IDeleteAccountUseCase deleteAccountUseCase,
        IUserScope userScope)
    {
        _subscribeTenantUseCase = subscribeTenantUseCase;
        _switchTenantUseCase = switchTenantUseCase;
        _listActiveSessionsQuery = listActiveSessionsQuery;
        _revokeSessionUseCase = revokeSessionUseCase;
        _deleteAccountUseCase = deleteAccountUseCase;
        _userScope = userScope;
    }

    /// <summary>
    /// Creates a tenant for the current OAuth application session (SaaS onboarding).
    /// </summary>
    /// <remarks>
    /// Returns a tenant-scoped JWT in <c>accessToken</c> (<c>token_use=tenant</c>) for immediate API calls.
    /// The OIDC platform token is unchanged and does not receive <c>tid</c>/<c>trole</c> claims.
    /// </remarks>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(SubscribeTenantResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscribeTenantResponse>> SubscribeTenant([FromBody] SubscribeTenantRequest request, CancellationToken ct)
    {
        var result = await _subscribeTenantUseCase.ExecuteAsync(request, ct);

        return Ok(new SubscribeTenantResponse
        {
            UserId = result.UserId,
            Email = result.Email,
            TenantId = result.TenantId,
            MembershipId = result.MembershipId,
            TenantRoles = result.TenantRoles,
            PlatformRoles = result.PlatformRoles,
            Tenants = result.Tenants,
            AccessToken = result.AccessToken,
            ExpiresIn = result.ExpiresIn,
            TokenType = result.TokenType
        });
    }

    /// <summary>
    /// Switches the active tenant for the current user session.
    /// </summary>
    /// <remarks>
    /// Returns a tenant-scoped JWT in <c>accessToken</c> for tenant API calls. Use the platform OIDC token as Bearer for this endpoint.
    /// </remarks>
    [HttpPost("switch-tenant")]
    [ProducesResponseType(typeof(TenantContextResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantContextResult>> SwitchTenant([FromBody] SwitchTenantRequest request, CancellationToken ct)
    {
        var result = await _switchTenantUseCase.ExecuteAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lists active authentication sessions for the current user.
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<AuthSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuthSessionDto>>> ListActiveSessions(CancellationToken ct)
    {
        var result = await _listActiveSessionsQuery.ExecuteAsync(_userScope.UserId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Revokes a single authentication session.
    /// </summary>
    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        await _revokeSessionUseCase.ExecuteAsync(_userScope.UserId, sessionId, ct);
        return NoContent();
    }

    /// <summary>
    /// Deletes the authenticated user's account for the current application tenant.
    /// </summary>
    [HttpDelete("account")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        await _deleteAccountUseCase.ExecuteAsync(ct);
        return NoContent();
    }
}
