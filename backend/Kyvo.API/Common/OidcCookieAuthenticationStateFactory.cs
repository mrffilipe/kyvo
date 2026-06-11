using System.Text.Json;
using Kyvo.Application.Services.Oidc;
using Microsoft.AspNetCore.Authentication;

namespace Kyvo.API.Common;

internal static class OidcCookieAuthenticationStateFactory
{
    public static OidcCookieAuthenticationState From(AuthenticateResult cookieAuth)
    {
        Guid? sessionId = null;
        if (cookieAuth.Succeeded && cookieAuth.Principal is not null)
        {
            var loginJson = cookieAuth.Principal.FindFirst("idp_login")?.Value;
            if (!string.IsNullOrWhiteSpace(loginJson))
            {
                try
                {
                    var context = JsonSerializer.Deserialize<OidcLoginContext>(loginJson);
                    sessionId = context?.SessionId;
                }
                catch (JsonException)
                {
                    sessionId = null;
                }
            }
        }

        return new OidcCookieAuthenticationState
        {
            Succeeded = cookieAuth.Succeeded,
            IssuedUtc = cookieAuth.Properties?.IssuedUtc,
            SessionId = sessionId
        };
    }
}
