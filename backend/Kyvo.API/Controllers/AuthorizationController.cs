using Kyvo.API.Models.Oidc;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Infrastructure.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using System.Security.Claims;

namespace Kyvo.API.Controllers;

/// <summary>
/// OAuth 2.0 authorization server and OpenID Connect provider endpoints under /connect. Thin controller on
/// top of OpenIddict Server: PKCE, authorization codes, refresh token rotation, discovery and JWKS are all
/// handled by OpenIddict itself. This controller only bridges OpenIddict to Kyvo's own domain (Identity
/// cookie -> AuthSession -> tenant/platform claims via <see cref="IKyvoClaimsPrincipalFactory"/>).
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = "oidc")]
[Tags("OAuth 2.0 / OpenID Connect")]
public sealed class AuthorizationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IKyvoClaimsPrincipalFactory _claimsFactory;
    private readonly IAuthSessionRepository _sessions;
    private readonly IApplicationClientRepository _clients;
    private readonly IUnitOfWork _unitOfWork;

    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        IKyvoClaimsPrincipalFactory claimsFactory,
        IAuthSessionRepository sessions,
        IApplicationClientRepository clients,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _claimsFactory = claimsFactory;
        _sessions = sessions;
        _clients = clients;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// OAuth 2.0 authorization endpoint (authorization code + PKCE).
    /// </summary>
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken ct)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (authenticateResult is not { Succeeded: true, Principal: not null })
        {
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + Request.QueryString
                });
        }

        var appUser = await ResolveApplicationUserAsync(authenticateResult.Principal)
            ?? throw new InvalidOperationException("The authenticated user could not be resolved.");

        if (!Guid.TryParse(authenticateResult.Principal.FindFirst("sid")?.Value, out var sessionId))
        {
            return ForbidWithError(OpenIddictConstants.Errors.LoginRequired, "Missing login session.");
        }

        var session = await _sessions.GetForUpdateAsync(sessionId, ct);
        if (session is null || session.Status != SessionStatus.Active)
        {
            return ForbidWithError(OpenIddictConstants.Errors.LoginRequired, "Session is no longer active.");
        }

        var client = await _clients.GetByClientIdAsync(request.ClientId!, ct);
        if (client is null)
        {
            return ForbidWithError(OpenIddictConstants.Errors.InvalidClient, "Unknown client_id.");
        }

        if (!session.ClientId.HasValue)
        {
            session.BindOAuthClient(client.Id);
        }

        try
        {
            var principal = await _claimsFactory.CreateAsync(UserMapper.ToDomain(appUser), session, request.ClientId!, request.GetScopes(), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (ForbiddenApplicationException ex)
        {
            return ForbidWithError(OpenIddictConstants.Errors.AccessDenied, ex.Message);
        }
    }

    /// <summary>
    /// OAuth 2.0 token endpoint (authorization_code and refresh_token grants).
    /// </summary>
    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exchange(CancellationToken ct)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            return ForbidWithError(OpenIddictConstants.Errors.UnsupportedGrantType, "Only authorization_code and refresh_token grants are supported.");
        }

        // The principal persisted when the authorization code/refresh token was issued (built in Authorize()
        // below) is retrieved here; OpenIddict itself already validated the client, the code/refresh token
        // and PKCE before this action runs.
        var authenticateResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (authenticateResult is not { Succeeded: true, Principal: not null })
        {
            return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, "The token is no longer valid.");
        }

        var appUser = await ResolveApplicationUserAsync(authenticateResult.Principal);
        if (appUser is null)
        {
            return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, "The authenticated user could not be resolved.");
        }

        if (!Guid.TryParse(authenticateResult.Principal.FindFirst("sid")?.Value, out var sessionId))
        {
            return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, "Missing login session.");
        }

        var session = await _sessions.GetForUpdateAsync(sessionId, ct);
        if (session is null || session.Status != SessionStatus.Active)
        {
            return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, "Session is no longer active.");
        }

        session.Touch();

        try
        {
            // Rebuilding claims (instead of just re-signing the stored principal) picks up tenant/role
            // changes that happened since the code/refresh token was issued (e.g. SwitchTenant, role edits).
            var principal = await _claimsFactory.CreateAsync(
                UserMapper.ToDomain(appUser),
                session,
                request.ClientId!,
                authenticateResult.Principal.GetScopes(),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (ForbiddenApplicationException ex)
        {
            return ForbidWithError(OpenIddictConstants.Errors.AccessDenied, ex.Message);
        }
    }

    /// <summary>
    /// OpenID Connect UserInfo endpoint (requires Bearer access token).
    /// </summary>
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [ProducesResponseType(typeof(OidcUserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<OidcUserInfoResponse> Userinfo()
    {
        return Ok(new OidcUserInfoResponse
        {
            Sub = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value,
            Email = User.FindFirst(OpenIddictConstants.Claims.Email)?.Value,
            Name = User.FindFirst(OpenIddictConstants.Claims.Name)?.Value,
            Tid = User.FindFirst("tid")?.Value,
            Mid = User.FindFirst("mid")?.Value,
            Trole = User.FindAll("trole").Select(c => c.Value).ToArray(),
            Prole = User.FindAll(Kyvo.Domain.Constants.PlatformRoleDefaults.CLAIM_TYPE).Select(c => c.Value).ToArray()
        });
    }

    /// <summary>
    /// OIDC RP-Initiated Logout (<c>end_session_endpoint</c>). Clears the Identity application cookie.
    /// </summary>
    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

        return SignOut(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = "/"
            });
    }

    private async Task<ApplicationUser?> ResolveApplicationUserAsync(ClaimsPrincipal principal)
    {
        var fromIdentity = await _userManager.GetUserAsync(principal);
        if (fromIdentity is not null)
        {
            return fromIdentity;
        }

        // OpenIddict principals carry the user id as "sub"/"uid", not ClaimTypes.NameIdentifier.
        var userIdValue = principal.FindFirst("uid")?.Value
            ?? principal.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;

        return Guid.TryParse(userIdValue, out var userId)
            ? await _userManager.FindByIdAsync(userId.ToString())
            : null;
    }

    private ForbidResult ForbidWithError(string error, string description)
    {
        var properties = new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        });

        return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
