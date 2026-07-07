using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using OpenIddict.Abstractions;

namespace Kyvo.API.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class ConsentController : ControllerBase
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IAntiforgery _antiforgery;

    public ConsentController(IOpenIddictApplicationManager applicationManager, IAntiforgery antiforgery)
    {
        _applicationManager = applicationManager;
        _antiforgery = antiforgery;
    }

    [HttpGet("~/connect/consent")]
    public async Task<IActionResult> Consent(CancellationToken ct)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!, ct);
        if (application is null)
        {
            return BadRequest("Unknown client.");
        }

        var applicationName = await _applicationManager.GetDisplayNameAsync(application, ct) ?? request.ClientId;
        var scopes = request.GetScopes().ToArray();
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        var html = $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
                <meta charset="utf-8" />
                <title>Consentimento</title>
            </head>
            <body>
                <h2>A aplicação <strong>{applicationName}</strong> deseja acessar:</h2>
                <form method="post" action="/connect/authorize{Request.QueryString}">
                    <input type="hidden" name="{tokens.FormFieldName}" value="{tokens.RequestToken}" />
                    <ul>
                        {string.Join("", scopes.Select(scope => $"<li><label><input type=\"checkbox\" name=\"scope\" value=\"{scope}\" checked /> {scope}</label></li>"))}
                    </ul>
                    <button type="submit" name="submit.Accept" value="Yes">Permitir</button>
                    <button type="submit" name="submit.Deny" value="No">Cancelar</button>
                </form>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }
}
