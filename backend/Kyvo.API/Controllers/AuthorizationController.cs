using Kyvo.API.Common;
using Kyvo.API.Models.Oidc;
using Kyvo.Application.Services.Oidc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Controllers;

/// <summary>
/// OAuth 2.0 authorization server and OpenID Connect provider endpoints under /connect.
/// </summary>
[ApiExplorerSettings(GroupName = "oidc")]
[Tags("OAuth 2.0 / OpenID Connect")]
public sealed class AuthorizationController : Controller
{
    private readonly IOidcAuthorizeHandler _authorizeHandler;
    private readonly IOidcTokenService _tokenService;
    private readonly IOidcClientValidator _clientValidator;
    private readonly IJwtSigningService _jwtSigning;

    public AuthorizationController(
        IOidcAuthorizeHandler authorizeHandler,
        IOidcTokenService tokenService,
        IOidcClientValidator clientValidator,
        IJwtSigningService jwtSigning)
    {
        _authorizeHandler = authorizeHandler;
        _tokenService = tokenService;
        _clientValidator = clientValidator;
        _jwtSigning = jwtSigning;
    }

    /// <summary>
    /// OAuth 2.0 authorization endpoint (authorization code + PKCE) via query string.
    /// </summary>
    /// <remarks>
    /// On success, redirects to <c>redirect_uri</c> with <c>code</c> and <c>state</c>. On failure, redirects with
    /// <c>error</c> and <c>error_description</c>, or returns a JSON error when <c>redirect_uri</c> is invalid.
    /// </remarks>
    [HttpGet("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(OidcErrorJsonResponse), StatusCodes.Status400BadRequest)]
    public Task<IActionResult> AuthorizeGet([FromQuery] OidcAuthorizeQueryRequest query, CancellationToken ct) =>
        AuthorizeAsync(query.ToOidcAuthorizeRequest(), OidcAuthorizeReturnUrl.FromQuery(Request), ct);

    /// <summary>
    /// OAuth 2.0 authorization endpoint (authorization code + PKCE) via form body.
    /// </summary>
    /// <remarks>
    /// Same behavior as GET; parameters are sent as <c>application/x-www-form-urlencoded</c>. On success, redirects to
    /// <c>redirect_uri</c> with <c>code</c> and <c>state</c>.
    /// </remarks>
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(OidcErrorJsonResponse), StatusCodes.Status400BadRequest)]
    public Task<IActionResult> AuthorizePost([FromForm] OidcAuthorizeFormRequest form, CancellationToken ct) =>
        AuthorizeAsync(form.ToOidcAuthorizeRequest(), OidcAuthorizeReturnUrl.FromForm(Request), ct);

    /// <summary>
    /// OAuth 2.0 token endpoint (authorization_code and refresh_token grants).
    /// </summary>
    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(OidcTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OidcErrorJsonResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OidcTokenResponse>> Exchange([FromForm] OidcTokenFormRequest form, CancellationToken ct)
    {
        var request = new OidcTokenRequest
        {
            GrantType = form.GrantType,
            Code = form.Code,
            RedirectUri = form.RedirectUri,
            ClientId = form.ClientId,
            ClientSecret = form.ClientSecret,
            CodeVerifier = form.CodeVerifier,
            RefreshToken = form.RefreshToken,
            Scope = form.Scope
        };

        var (response, error) = await _tokenService.ExchangeAsync(request, ct);
        if (error is not null)
        {
            return OidcOAuthResults.JsonError(error);
        }

        return Ok(response);
    }

    /// <summary>
    /// OpenID Connect UserInfo endpoint (requires Bearer access token).
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(OidcUserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<OidcUserInfoResponse> Userinfo()
    {
        return Ok(new OidcUserInfoResponse
        {
            Sub = User.FindFirst(OidcConstants.Claims.Subject)?.Value,
            Email = User.FindFirst(OidcConstants.Claims.Email)?.Value,
            Name = User.FindFirst(OidcConstants.Claims.Name)?.Value,
            Tid = User.FindFirst("tid")?.Value,
            Mid = User.FindFirst("mid")?.Value,
            Trole = User.FindAll("trole").Select(c => c.Value).ToArray(),
            Prole = User.FindFirst("prole")?.Value
        });
    }

    /// <summary>
    /// OIDC RP-Initiated Logout (<c>end_session_endpoint</c>). Clears the browser cookie session without
    /// requiring authentication; redirects to <c>post_logout_redirect_uri</c> only when it is registered for the client.
    /// </summary>
    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Logout(
        [FromQuery(Name = "client_id")] string? clientId,
        [FromQuery(Name = "post_logout_redirect_uri")] string? postLogoutRedirectUri,
        [FromQuery(Name = "id_token_hint")] string? idTokenHint,
        CancellationToken ct)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (string.IsNullOrWhiteSpace(postLogoutRedirectUri))
        {
            return Redirect("/");
        }

        var resolvedClientId = clientId ?? IdTokenHintClientIdResolver.TryResolveClientId(idTokenHint, _jwtSigning);
        if (string.IsNullOrWhiteSpace(resolvedClientId))
        {
            return Redirect("/");
        }

        var (client, _) = await _clientValidator.ValidateClientAsync(resolvedClientId, null, ct);
        if (client is null)
        {
            return Redirect("/");
        }

        var redirectError = _clientValidator.ValidatePostLogoutRedirectUri(client, postLogoutRedirectUri);
        if (redirectError is not null)
        {
            return Redirect("/");
        }

        return Redirect(postLogoutRedirectUri);
    }

    private async Task<IActionResult> AuthorizeAsync(OidcAuthorizeRequest request, string challengeReturnUrl, CancellationToken ct)
    {
        var cookieAuth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var authState = OidcCookieAuthenticationStateFactory.From(cookieAuth);
        var outcome = await _authorizeHandler.HandleAsync(request, authState, ct);
        return OidcOAuthResults.FromAuthorizeOutcome(outcome, challengeReturnUrl);
    }
}
