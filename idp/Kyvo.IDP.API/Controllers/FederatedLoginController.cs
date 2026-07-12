using Kyvo.IDP.Application.UseCases.ExternalLogin;
using Kyvo.IDP.Infrastructure.Extensions;
using Kyvo.IDP.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenIddict.Client.AspNetCore;

namespace Kyvo.IDP.API.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class FederatedLoginController : ControllerBase
{
    private readonly IExternalLoginUseCase _externalLogin;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<FederatedLoginController> _logger;

    public FederatedLoginController(
        IExternalLoginUseCase externalLogin,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<FederatedLoginController> logger)
    {
        _externalLogin = externalLogin;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet("/login/federated/google")]
    public IActionResult ChallengeGoogle(
        [FromQuery] string? returnUrl,
        [FromServices] IConfiguration configuration)
    {
        var clientId = configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            _logger.LogWarning("Google federated login requested but Google:ClientId is not configured");
            return RedirectToLogin(returnUrl, "invalid_provider");
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(CallbackGoogle), new { returnUrl })
        };

        properties.Items[OpenIddictClientAspNetCoreConstants.Properties.ProviderName] =
            OpenIddictClientServiceCollectionExtensions.GoogleRegistrationId;

        return Challenge(properties, OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("/callback/login/google")]
    [HttpPost("/callback/login/google")]
    public async Task<IActionResult> CallbackGoogle([FromQuery] string? returnUrl, CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);
        if (result is not { Succeeded: true, Principal: not null })
        {
            _logger.LogWarning("Google federated callback authentication failed");
            return RedirectToLogin(returnUrl, "invalid_provider");
        }

        try
        {
            var link = await _externalLogin.ExecuteAsync(
                result.Principal,
                OpenIddictClientServiceCollectionExtensions.GoogleRegistrationId,
                ct);

            var user = await _userManager.FindByIdAsync(link.UserId.ToString());
            if (user is null || !user.IsActive)
            {
                return RedirectToLogin(returnUrl, "invalid_provider");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation(
                "Federated Google login succeeded for {UserId} (created={Created}, linked={Linked})",
                user.Id,
                link.Created,
                link.Linked);

            return LocalRedirect(SafeReturnUrl(returnUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Federated Google login failed during account linking");
            return RedirectToLogin(returnUrl, "invalid_provider");
        }
    }

    private static string SafeReturnUrl(string? returnUrl) =>
        string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/') || returnUrl.StartsWith("//", StringComparison.Ordinal)
            ? "/"
            : returnUrl;

    private IActionResult RedirectToLogin(string? returnUrl, string errorCode) =>
        Redirect($"/account/login{QueryString.Create(new Dictionary<string, string?>
        {
            ["returnUrl"] = returnUrl,
            ["error"] = errorCode
        }.Where(p => !string.IsNullOrWhiteSpace(p.Value)))}");
}
