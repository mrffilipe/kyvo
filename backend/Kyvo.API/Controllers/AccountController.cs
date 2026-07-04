using Kyvo.API.Models.Oidc;
using Kyvo.API.Services;
using Kyvo.Application.Ports.Identity;
using Kyvo.Application.UseCases.Auth;
using Kyvo.Application.UseCases.Auth.ExternalLogin;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;
using Kyvo.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kyvo.API.Controllers;

/// <summary>
/// Browser-based sign-in and sign-out (form posts from Blazor login UI). Not part of the versioned JSON API.
/// Local login/registration is delegated to ASP.NET Core Identity's <see cref="SignInManager{TUser}"/>
/// and <see cref="UserManager{TUser}"/> (password hashing, lockout, uniqueness).
/// </summary>
[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(GroupName = "oidc")]
[Tags("Account (browser login)")]
public sealed class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUserAccountService _userAccounts;
    private readonly ITenantMembershipRepository _memberships;
    private readonly IUserPlatformRoleRepository _userPlatformRoles;
    private readonly IAccountSignInService _accountSignInService;
    private readonly IAntiforgery _antiforgery;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserAccountService userAccounts,
        ITenantMembershipRepository memberships,
        IUserPlatformRoleRepository userPlatformRoles,
        IAccountSignInService accountSignInService,
        IAntiforgery antiforgery)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userAccounts = userAccounts;
        _memberships = memberships;
        _userPlatformRoles = userPlatformRoles;
        _accountSignInService = accountSignInService;
        _antiforgery = antiforgery;
    }

    /// <summary>
    /// Local email/password sign-in; establishes an authentication cookie for the OAuth authorize flow.
    /// </summary>
    /// <remarks>
    /// On failure, redirects to <c>/account/login</c> with an <c>error</c> query code
    /// (<c>invalid_credentials</c>, <c>missing_fields</c>, <c>session_expired</c>, <c>locked_out</c>, etc.).
    /// </remarks>
    [HttpPost("/account/signin")]
    [IgnoreAntiforgeryToken]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Login([FromForm] AccountLoginForm form, CancellationToken ct)
    {
        var antiforgeryFailure = await TryRedirectOnAntiforgeryFailureAsync(form.ReturnUrl, ct);
        if (antiforgeryFailure is not null)
        {
            return antiforgeryFailure;
        }

        if (string.IsNullOrWhiteSpace(form.Email) || string.IsNullOrWhiteSpace(form.Password))
        {
            return RedirectToLogin(form.ReturnUrl, "missing_fields");
        }

        var appUser = await _userManager.FindByEmailAsync(form.Email.Trim());
        if (appUser is null || !appUser.IsActive)
        {
            return RedirectToLogin(form.ReturnUrl, "invalid_credentials");
        }

        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(appUser, form.Password, lockoutOnFailure: true);
        if (passwordCheck.IsLockedOut)
        {
            return RedirectToLogin(form.ReturnUrl, "locked_out");
        }

        if (!passwordCheck.Succeeded)
        {
            return RedirectToLogin(form.ReturnUrl, "invalid_credentials");
        }

        var login = await BuildExternalLoginResultAsync(UserMapper.ToDomain(appUser), ct);
        return await CompleteLoginAsync(login, form.ReturnUrl, ct);
    }

    /// <summary>
    /// Self-registration; creates the user and establishes an authentication cookie for OAuth authorize.
    /// </summary>
    [HttpPost("/account/register")]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("account_register")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Register([FromForm] AccountRegisterForm form, CancellationToken ct)
    {
        var antiforgeryFailure = await TryRedirectOnAntiforgeryFailureAsync(
            form.ReturnUrl,
            ct,
            register: true);
        if (antiforgeryFailure is not null)
        {
            return antiforgeryFailure;
        }

        if (!string.Equals(form.Password, form.PasswordConfirm, StringComparison.Ordinal))
        {
            return RedirectToRegister(form.ReturnUrl, "password_mismatch");
        }

        if (string.IsNullOrWhiteSpace(form.Email) || string.IsNullOrWhiteSpace(form.DisplayName))
        {
            return RedirectToRegister(form.ReturnUrl, "validation");
        }

        User user;
        try
        {
            user = new User(new EmailAddress(form.Email), form.DisplayName);
        }
        catch (DomainValidationException ex)
        {
            return RedirectToRegister(form.ReturnUrl, "validation", ex.Message);
        }

        var createResult = await _userAccounts.CreateWithPasswordAsync(user, form.Password, ct);
        if (!createResult.Succeeded)
        {
            var code = createResult.Errors.Any(e =>
                e.Contains("email", StringComparison.OrdinalIgnoreCase) ||
                e.Contains("user name", StringComparison.OrdinalIgnoreCase))
                ? "email_exists"
                : "registration_failed";
            return RedirectToRegister(form.ReturnUrl, code, string.Join(" ", createResult.Errors));
        }

        var login = await BuildExternalLoginResultAsync(user, ct);
        return await CompleteLoginAsync(login, form.ReturnUrl, ct);
    }

    /// <summary>
    /// Signs out the browser session cookie.
    /// </summary>
    [Authorize(AuthenticationSchemes = "Identity.Application")]
    [HttpPost("/account/logout")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!await _antiforgery.IsRequestValidAsync(HttpContext))
        {
            return Redirect("/account/login");
        }

        await _signInManager.SignOutAsync();
        return Redirect("/");
    }

    private async Task<ExternalLoginResult> BuildExternalLoginResultAsync(User user, CancellationToken ct)
    {
        var memberships = await _memberships.ListByUserIdWithTenantAndRolesAsync(user.Id, ct);
        var platformRoleAssignments = await _userPlatformRoles.ListByUserIdAsync(user.Id, ct);

        return new ExternalLoginResult
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            PlatformRoles = platformRoleAssignments.Select(x => x.Role.Key).ToList(),
            TenantMemberships = memberships
                .Select(m => new ExternalLoginTenantMembership
                {
                    TenantId = m.TenantId,
                    MembershipId = m.Id,
                    Roles = m.Roles.Select(r => r.Role.Key.Value).ToList()
                })
                .ToList()
        };
    }

    private async Task<IActionResult?> TryRedirectOnAntiforgeryFailureAsync(string? returnUrl, CancellationToken ct, bool register = false)
    {
        if (await _antiforgery.IsRequestValidAsync(HttpContext))
        {
            return null;
        }

        return register
            ? RedirectToRegister(returnUrl, "session_expired")
            : RedirectToLogin(returnUrl, "session_expired");
    }

    private IActionResult RedirectToRegister(string? returnUrl, string errorCode, string? detail = null)
    {
        var query = QueryString.Create(new Dictionary<string, string?>
        {
            ["returnUrl"] = returnUrl,
            ["error"] = errorCode,
            ["error_description"] = detail
        }.Where(p => !string.IsNullOrWhiteSpace(p.Value)));

        return Redirect($"/account/register{query}");
    }

    private IActionResult RedirectToLogin(string? returnUrl, string errorCode)
    {
        var query = QueryString.Create(new Dictionary<string, string?>
        {
            ["returnUrl"] = returnUrl,
            ["error"] = errorCode
        }.Where(p => !string.IsNullOrWhiteSpace(p.Value)));
        return Redirect($"/account/login{query}");
    }

    private async Task<IActionResult> CompleteLoginAsync(ExternalLoginResult login, string? returnUrl, CancellationToken ct)
    {
        await _accountSignInService.SignInAsync(HttpContext, login, ct);
        var redirectTarget = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return LocalRedirect(redirectTarget);
    }
}
