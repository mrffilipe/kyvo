using Kyvo.API.Models.Oidc;
using Kyvo.Application.Services.Oidc;
using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Common;

internal static class OidcOAuthResults
{
    public static IActionResult RedirectError(string? redirectUri, string? state, OidcError error)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return JsonError(error);
        }

        var query = QueryString.Create(new Dictionary<string, string?>
        {
            ["error"] = error.Error,
            ["error_description"] = error.ErrorDescription,
            ["state"] = state
        }.Where(x => !string.IsNullOrWhiteSpace(x.Value)));

        return new RedirectResult($"{redirectUri}{query}");
    }

    public static ActionResult JsonError(OidcError error) =>
        new BadRequestObjectResult(new OidcErrorJsonResponse
        {
            Error = error.Error,
            ErrorDescription = error.ErrorDescription
        });

    public static IActionResult FromAuthorizeOutcome(OidcAuthorizeOutcome outcome, string challengeReturnUrl)
    {
        return outcome switch
        {
            OidcAuthorizeSuccess success => SuccessRedirect(success),
            OidcAuthorizeRedirectError error => RedirectError(error.RedirectUri, error.State, error.Error),
            OidcAuthorizeLoginChallenge => new ChallengeResult(
                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = challengeReturnUrl }),
            _ => throw new InvalidOperationException($"Unknown authorize outcome: {outcome.GetType().Name}")
        };
    }

    private static IActionResult SuccessRedirect(OidcAuthorizeSuccess success)
    {
        var redirect = QueryString.Create(new Dictionary<string, string?>
        {
            ["code"] = success.Code,
            ["state"] = success.State
        }.Where(x => !string.IsNullOrWhiteSpace(x.Value)));

        return new RedirectResult($"{success.RedirectUri}{redirect}");
    }
}
