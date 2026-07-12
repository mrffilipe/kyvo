using Kyvo.IDP.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Kyvo.IDP.API.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAntiforgery antiforgery,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    [HttpPost("/account/signin")]
    [IgnoreAntiforgeryToken]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> SignIn(
        [FromForm] string? email,
        [FromForm] string? password,
        [FromForm] string? returnUrl,
        CancellationToken ct)
    {
        if (!await IsAntiforgeryValidAsync())
        {
            return RedirectToLogin(returnUrl, "session_expired");
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return RedirectToLogin(returnUrl, "missing_fields");
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive)
        {
            return RedirectToLogin(returnUrl, "invalid_credentials");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return RedirectToLogin(returnUrl, "locked_out");
        }

        if (!result.Succeeded)
        {
            return RedirectToLogin(returnUrl, "invalid_credentials");
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("Local sign-in succeeded for {UserId}", user.Id);

        return LocalRedirect(SafeReturnUrl(returnUrl));
    }

    [HttpPost("/account/register")]
    [IgnoreAntiforgeryToken]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Register(
        [FromForm] string? email,
        [FromForm] string? displayName,
        [FromForm] string? password,
        [FromForm] string? passwordConfirm,
        [FromForm] string? returnUrl,
        CancellationToken ct)
    {
        if (!await IsAntiforgeryValidAsync())
        {
            return RedirectToRegister(returnUrl, "session_expired");
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(password))
        {
            return RedirectToRegister(returnUrl, "validation");
        }

        if (!string.Equals(password, passwordConfirm, StringComparison.Ordinal))
        {
            return RedirectToRegister(returnUrl, "password_mismatch");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email.Trim(),
            Email = email.Trim(),
            EmailConfirmed = false,
            DisplayName = displayName.Trim(),
            IsActive = true
        };
        user.SetCreatedAt();

        var create = await _userManager.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            var emailExists = create.Errors.Any(e =>
                e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)
                || e.Description.Contains("email", StringComparison.OrdinalIgnoreCase));
            return RedirectToRegister(returnUrl, emailExists ? "email_exists" : "registration_failed");
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("Registered and signed in local user {UserId}", user.Id);

        return LocalRedirect(SafeReturnUrl(returnUrl));
    }

    [HttpPost("/account/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout([FromForm] string? returnUrl)
    {
        await _signInManager.SignOutAsync();
        return LocalRedirect(SafeReturnUrl(returnUrl));
    }

    private async Task<bool> IsAntiforgeryValidAsync()
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    private static string SafeReturnUrl(string? returnUrl) =>
        string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/') || returnUrl.StartsWith("//", StringComparison.Ordinal)
            ? "/"
            : returnUrl;

    private IActionResult RedirectToLogin(string? returnUrl, string error) =>
        Redirect($"/account/login{QueryString.Create(new Dictionary<string, string?>
        {
            ["returnUrl"] = returnUrl,
            ["error"] = error
        }.Where(p => !string.IsNullOrWhiteSpace(p.Value)))}");

    private IActionResult RedirectToRegister(string? returnUrl, string error) =>
        Redirect($"/account/register{QueryString.Create(new Dictionary<string, string?>
        {
            ["returnUrl"] = returnUrl,
            ["error"] = error
        }.Where(p => !string.IsNullOrWhiteSpace(p.Value)))}");
}
