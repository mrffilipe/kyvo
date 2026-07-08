using System.Collections.Immutable;
using System.Security.Claims;
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
using Kyvo.Domain.Constants;

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
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;

    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
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

        var user = await _userManager.GetUserAsync(authenticateResult.Principal)
            ?? throw new InvalidOperationException("The authenticated user could not be resolved.");

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
                    subject: user.Id.ToString("D"),
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

        var scopes = request.GetScopes();
        var principal = await CreateOpenIddictPrincipalAsync(
            user,
            authenticateResult.Principal,
            request.ClientId!,
            scopes,
            ct);

        if (isConsentSubmission)
        {
            var clientId = await _applicationManager.GetIdAsync(application, ct)
                ?? throw new InvalidOperationException("OpenIddict client id is missing.");

            var authorization = await _authorizationManager.CreateAsync(
                principal: principal,
                subject: user.Id.ToString("D"),
                client: clientId,
                type: OpenIddictConstants.AuthorizationTypes.Permanent,
                scopes: principal.GetScopes(),
                ct);

            principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization, ct));
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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

        var user = await _userManager.GetUserAsync(authenticateResult.Principal);
        if (user is null)
        {
            return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, "The authenticated user could not be resolved.");
        }

        var scopes = authenticateResult.Principal.GetScopes();
        var principal = await CreateOpenIddictPrincipalAsync(
            user,
            authenticateResult.Principal,
            request.ClientId!,
            scopes,
            ct);

        var authorizationId = authenticateResult.Principal.GetAuthorizationId();
        if (!string.IsNullOrEmpty(authorizationId))
        {
            principal.SetAuthorizationId(authorizationId);
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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
            Name = User.FindFirst(OpenIddictConstants.Claims.Name)?.Value
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

    private async Task<ClaimsPrincipal> CreateOpenIddictPrincipalAsync(
        ApplicationUser user,
        ClaimsPrincipal sourcePrincipal,
        string clientId,
        ImmutableArray<string> scopes,
        CancellationToken ct)
    {
        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        CopyClaim(sourcePrincipal, principal, "sid");
        principal.SetClaim("client_id", clientId);

        principal.SetScopes(scopes);

        var resources = new List<string>();
        await foreach (var resource in _scopeManager.ListResourcesAsync(scopes, ct))
        {
            resources.Add(resource);
        }

        principal.SetResources(resources);
        SetClaimDestinations(principal);

        return principal;
    }

    private static void CopyClaim(ClaimsPrincipal source, ClaimsPrincipal target, string claimType)
    {
        var claim = source.FindFirst(claimType);
        if (claim is not null)
        {
            target.SetClaim(claimType, claim.Value);
        }
    }

    private static void SetClaimDestinations(ClaimsPrincipal principal)
    {
        principal.SetDestinations(static claim => claim.Type switch
        {
            OpenIddictConstants.Claims.Name when claim.Subject.HasScope(OpenIddictConstants.Scopes.Profile) =>
            [
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            ],
            OpenIddictConstants.Claims.Email when claim.Subject.HasScope(OpenIddictConstants.Scopes.Email) =>
            [
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            ],
            OpenIddictConstants.Claims.Name =>
            [OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Email =>
            [OpenIddictConstants.Destinations.IdentityToken],
            OpenIddictConstants.Claims.Subject =>
            [
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            ],
            "sid" or PlatformRoleDefaults.CLAIM_TYPE or "client_id" =>
            [OpenIddictConstants.Destinations.AccessToken],
            _ => [OpenIddictConstants.Destinations.AccessToken]
        });
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
