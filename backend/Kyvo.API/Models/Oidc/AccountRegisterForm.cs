using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Models.Oidc;

/// <summary>
/// Form body for self-registration (application/x-www-form-urlencoded).
/// </summary>
public sealed class AccountRegisterForm
{
    [FromForm]
    public string DisplayName { get; set; } = string.Empty;

    [FromForm]
    public string Email { get; set; } = string.Empty;

    [FromForm]
    public string Password { get; set; } = string.Empty;

    [FromForm]
    public string PasswordConfirm { get; set; } = string.Empty;

    [FromForm]
    public string? ReturnUrl { get; set; }
}
