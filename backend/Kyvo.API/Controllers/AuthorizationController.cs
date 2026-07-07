using System.Collections.Immutable;
using System.Security.Claims;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;
using Kyvo.Infrastructure.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using Kyvo.API.Models.Oidc;

namespace Kyvo.API.Controllers;

/// <summary>
/// OAuth 2.0 authorization server and OpenID Connect provider endpoints under /connect.
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = "oidc")]
[Tags("OAuth 2.0 / OpenID Connect")]
public sealed class AuthorizationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOidcClaimsPrincipalFactory _claimsFactory;
    private readonly IAuthSessionRepository _sessions;
    private readonly IOAuthClientManager _oauthClients;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IUnitOfWork _unitOfWork;

    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        IOidcClaimsPrincipalFactory claimsFactory,
        IAuthSessionRepository sessions,
        IOAuthClientManager oauthClients,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _claimsFactory = claimsFactory;
        _sessions = sessions;
        _oauthClients = oauthClients;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize(CancellationToken ct)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        if (HttpMethods.IsPost(Request.Method) && Request.HasFormContentType && Request.Form.ContainsKey("submit.Deny"))
        {
            return ForbidWithError(OpenIddictConstants.Errors.AccessDenied, "The user denied the authorization request.");
        }

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

        var client = await _oauthClients.GetByClientIdAsync(request.ClientId!, ct);
        if (client is null)
        {
            return ForbidWithError(OpenIddictConstants.Errors.InvalidClient, "Unknown client_id.");
        }

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!, ct);
        if (application is null)
        {
            return ForbidWithError(OpenIddictConstants.Errors.InvalidClient, "Unknown client_id.");
        }

        var isConsentSubmission = HttpMethods.IsPost(Request.Method)
            && Request.HasFormContentType
            && Request.Form.ContainsKey("submit.Accept");

        if (!isConsentSubmission)
        {
            var consentType = await _applicationManager.GetConsentTypeAsync(application, ct);
            if (!string.Equals(consentType, OpenIddictConstants.ConsentTypes.Implicit, StringComparison.Ordinal))
            {
                var hasPermanentAuthorization = false;
                await foreach (var _ in _authorizationManager.FindAsync(
                    subject: appUser.Id.ToString("D"),
                    client: await _applicationManager.GetIdAsync(application, ct),
                    status: OpenIddictConstants.Statuses.Valid,
                    type: OpenIddictConstants.AuthorizationTypes.Permanent,
                    scopes: request.GetScopes()).WithCancellation(ct))
                {
                    hasPermanentAuthorization = true;
                    break;
                }

                if (!hasPermanentAuthorization && !request.HasPromptValue(OpenIddictConstants.PromptValues.None))
                {
                    return Redirect($"/connect/consent{Request.QueryString}");
                }
            }
        }

        try
        {
            var principal = await _claimsFactory.CreateAsync(
                UserMapper.ToDomain(appUser),
                session,
                request.ClientId!,
                request.GetScopes(),
                ct);

            ApplyAccessTokenLifetime(principal, client.AccessTokenTtlSeconds);

            if (isConsentSubmission)
            {
                var clientId = await _applicationManager.GetIdAsync(application, ct)
                    ?? throw new InvalidOperationException("OpenIddict client id is missing.");

                var authorization = await _authorizationManager.CreateAsync(
                    principal: principal,
                    subject: appUser.Id.ToString("D"),
                    client: clientId,
                    type: OpenIddictConstants.AuthorizationTypes.Permanent,
                    scopes: principal.GetScopes(),
                    ct);

                principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization, ct));
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (ForbiddenApplicationException ex)
        {
            return ForbidWithError(OpenIddictConstants.Errors.AccessDenied, ex.Message);
        }
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("connect_token")]
    public async Task<IActionResult> Exchange(CancellationToken ct)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            return ForbidWithError(OpenIddictConstants.Errors.UnsupportedGrantType, "Only authorization_code and refresh_token grants are supported.");
        }

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

        var client = await _oauthClients.GetByClientIdAsync(request.ClientId!, ct);

        try
        {
            var principal = await _claimsFactory.CreateAsync(
                UserMapper.ToDomain(appUser),
                session,
                request.ClientId!,
                authenticateResult.Principal.GetScopes(),
                ct);

            if (client is not null)
            {
                ApplyAccessTokenLifetime(principal, client.AccessTokenTtlSeconds);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (ForbiddenApplicationException ex)
        {
            return ForbidWithError(OpenIddictConstants.Errors.AccessDenied, ex.Message);
        }
    }

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
            Sid = User.FindFirst("sid")?.Value,
            Prole = User.FindAll(Kyvo.Domain.Constants.PlatformRoleDefaults.CLAIM_TYPE).Select(c => c.Value).ToArray()
        });
    }

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

    private static void ApplyAccessTokenLifetime(ClaimsPrincipal principal, int accessTokenTtlSeconds)
    {
        if (accessTokenTtlSeconds > 0)
        {
            principal.SetAccessTokenLifetime(TimeSpan.FromSeconds(accessTokenTtlSeconds));
        }
    }

    private async Task<ApplicationUser?> ResolveApplicationUserAsync(ClaimsPrincipal principal)
    {
        var fromIdentity = await _userManager.GetUserAsync(principal);
        if (fromIdentity is not null)
        {
            return fromIdentity;
        }

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
