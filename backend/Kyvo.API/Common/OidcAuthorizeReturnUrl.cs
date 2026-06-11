using Microsoft.AspNetCore.Http;

namespace Kyvo.API.Common;

internal static class OidcAuthorizeReturnUrl
{
    public static string FromQuery(HttpRequest request) =>
        request.PathBase + request.Path + request.QueryString;

    public static string FromForm(HttpRequest request)
    {
        var query = request.Form.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value.ToString()));
        return request.PathBase + request.Path + QueryString.Create(query);
    }
}
