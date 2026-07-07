namespace Kyvo.API.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Require CSP on specific paths (e.g., account, login, connect)
        if (context.Request.Path.StartsWithSegments("/account") ||
            context.Request.Path.StartsWithSegments("/connect") ||
            context.Request.Path.StartsWithSegments("/login"))
        {
            var csp = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';";
            context.Response.Headers.Append("Content-Security-Policy", csp);
        }

        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        await _next(context);
    }
}
