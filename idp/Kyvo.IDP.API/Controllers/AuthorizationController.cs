using System.Collections.Immutable;
using System.Security.Claims;
using Kyvo.IDP.Infrastructure.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace Kyvo.IDP.API.Controllers;

[ApiController]
public sealed class AuthorizationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;

    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager)
    {
        _userManager = userManager;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
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

        if (!user.IsActive)
        {
            return ForbidWithError(OpenIddictConstants.Errors.AccessDenied, "The user account is inactive.");
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
        var principal = await CreateOpenIddictPrincipalAsync(user, request.ClientId!, scopes);

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
        if (user is null || !user.IsActive)
        {
            return ForbidWithError(OpenIddictConstants.Errors.InvalidGrant, "The authenticated user could not be resolved.");
        }

        var principal = await CreateOpenIddictPrincipalAsync(user, request.ClientId!, authenticateResult.Principal.GetScopes());

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
    public IActionResult Userinfo()
    {
        var claims = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [OpenIddictConstants.Claims.Subject] = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value
        };

        if (User.HasScope(OpenIddictConstants.Scopes.Email))
        {
            claims[OpenIddictConstants.Claims.Email] = User.FindFirst(OpenIddictConstants.Claims.Email)?.Value;
            claims[OpenIddictConstants.Claims.EmailVerified] = User.FindFirst(OpenIddictConstants.Claims.EmailVerified)?.Value;
        }

        if (User.HasScope(OpenIddictConstants.Scopes.Profile))
        {
            claims[OpenIddictConstants.Claims.Name] = User.FindFirst(OpenIddictConstants.Claims.Name)?.Value;
            claims[OpenIddictConstants.Claims.Picture] = User.FindFirst(OpenIddictConstants.Claims.Picture)?.Value;
        }

        return Ok(claims);
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
        string clientId,
        ImmutableArray<string> scopes)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString("D"))
                .SetClaim(OpenIddictConstants.Claims.Email, user.Email)
                .SetClaim(OpenIddictConstants.Claims.EmailVerified, user.EmailConfirmed.ToString().ToLowerInvariant())
                .SetClaim(OpenIddictConstants.Claims.Name, user.DisplayName)
                .SetClaim(OpenIddictConstants.Claims.PreferredUsername, user.UserName);

        if (!string.IsNullOrWhiteSpace(user.PhotoUrl))
        {
            identity.SetClaim(OpenIddictConstants.Claims.Picture, user.PhotoUrl);
        }

        identity.SetClaim("client_id", clientId);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);
        principal.SetResources("kyvo-idp");
        SetClaimDestinations(principal);

        await Task.CompletedTask;
        return principal;
    }

    private static void SetClaimDestinations(ClaimsPrincipal principal)
    {
        principal.SetDestinations(static claim => claim.Type switch
        {
            OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Picture
                when claim.Subject!.HasScope(OpenIddictConstants.Scopes.Profile) =>
            [
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            ],
            OpenIddictConstants.Claims.Email or OpenIddictConstants.Claims.EmailVerified
                when claim.Subject!.HasScope(OpenIddictConstants.Scopes.Email) =>
            [
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            ],
            OpenIddictConstants.Claims.Subject or OpenIddictConstants.Claims.PreferredUsername =>
            [
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.IdentityToken
            ],
            "client_id" => [OpenIddictConstants.Destinations.AccessToken],
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

// Avoid bringing Microsoft.IdentityModel into every file for the auth type constant.
file static class TokenValidationParameters
{
    public const string DefaultAuthenticationType = "Bearer";
}
