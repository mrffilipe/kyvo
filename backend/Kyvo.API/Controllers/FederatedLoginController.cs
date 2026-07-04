using Kyvo.API.Services;
using Kyvo.Application.UseCases.Auth.ExternalLogin;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Client.AspNetCore;

namespace Kyvo.API.Controllers;

/// <summary>
/// Redirect-based federated login (Google, Microsoft, GitHub via OpenIddict.Client.WebIntegration, or any
/// admin-configured generic OIDC provider). The browser is redirected to the upstream provider and back;
/// token validation is handled entirely by OpenIddict.Client.
/// </summary>
[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(GroupName = "oidc")]
[Tags("Federated login")]
public sealed class FederatedLoginController : ControllerBase
{
    private readonly IIdentityProviderRepository _identityProviders;
    private readonly IExternalLoginUseCase _externalLoginUseCase;
    private readonly IAccountSignInService _accountSignInService;

    public FederatedLoginController(
        IIdentityProviderRepository identityProviders,
        IExternalLoginUseCase externalLoginUseCase,
        IAccountSignInService accountSignInService)
    {
        _identityProviders = identityProviders;
        _externalLoginUseCase = externalLoginUseCase;
        _accountSignInService = accountSignInService;
    }

    /// <summary>
    /// Starts the federated login flow for <paramref name="alias"/> (redirects to the upstream provider).
    /// </summary>
    [HttpGet("/login/federated/{alias}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Challenge(string alias, [FromQuery] string? returnUrl, CancellationToken ct)
    {
        var provider = await _identityProviders.GetEnabledByAliasAsync(alias, ct);
        if (provider is null || provider.ProviderType == IdentityProviderType.Local)
        {
            return RedirectToLogin(returnUrl, "invalid_provider");
        }

        var properties = new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictClientAspNetCoreConstants.Properties.RegistrationId] = alias
        })
        {
            RedirectUri = Url.Action(nameof(Callback), new { alias, returnUrl })
        };

        return Challenge(properties, OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Callback hit by OpenIddict.Client after the upstream provider redirects back with the authorization
    /// response. By this point the token exchange and validation already happened inside OpenIddict.Client;
    /// this action only maps the resulting claims onto a local <c>User</c>.
    /// </summary>
    [HttpGet("/callback/login/{alias}")]
    [HttpPost("/callback/login/{alias}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(string alias, [FromQuery] string? returnUrl, CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);
        if (result is not { Succeeded: true, Principal: not null })
        {
            return RedirectToLogin(returnUrl, "invalid_provider");
        }

        var providerUserId = FindClaim(result.Principal, OpenIddictConstants.Claims.Subject, System.Security.Claims.ClaimTypes.NameIdentifier);
        var email = FindClaim(result.Principal, OpenIddictConstants.Claims.Email, System.Security.Claims.ClaimTypes.Email);
        var name = FindClaim(result.Principal, OpenIddictConstants.Claims.Name, System.Security.Claims.ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(providerUserId) || string.IsNullOrWhiteSpace(email))
        {
            return RedirectToLogin(returnUrl, "invalid_provider");
        }

        ExternalLoginResult login;
        try
        {
            login = await _externalLoginUseCase.ExecuteAsync(
                new ExternalLoginRequest
                {
                    ProviderAlias = alias,
                    ProviderUserId = providerUserId,
                    Email = email,
                    DisplayName = name
                },
                ct);
        }
        catch (Exception ex) when (ex is Domain.Exceptions.DomainBusinessRuleException
            or Domain.Exceptions.DomainNotFoundException
            or Domain.Exceptions.DomainValidationException)
        {
            return RedirectToLogin(returnUrl, "invalid_provider");
        }

        await _accountSignInService.SignInAsync(HttpContext, login, ct);
        var redirectTarget = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return LocalRedirect(redirectTarget);
    }

    private static string? FindClaim(System.Security.Claims.ClaimsPrincipal principal, params string[] types) =>
        types.Select(principal.FindFirst).FirstOrDefault(c => c is not null)?.Value;

    private IActionResult RedirectToLogin(string? returnUrl, string errorCode)
    {
        var query = QueryString.Create(new Dictionary<string, string?>
        {
            ["returnUrl"] = returnUrl,
            ["error"] = errorCode
        }.Where(p => !string.IsNullOrWhiteSpace(p.Value)));
        return Redirect($"/account/login{query}");
    }
}
