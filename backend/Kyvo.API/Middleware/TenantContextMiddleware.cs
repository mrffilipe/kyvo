using Kyvo.Application.Services.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Kyvo.API.Middleware;

/// <summary>
/// Resolves tenant id from the authenticated principal's <c>tid</c> claim (tenant JWT only).
/// </summary>
public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var tid = context.User.FindFirst("tid")?.Value;
        if (Guid.TryParse(tid, out var tenantId))
        {
            tenantContext.SetTenant(tenantId);
        }

        await _next(context);
    }
}
