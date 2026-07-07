using Kyvo.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kyvo.Client;

public static class KyvoClientServiceCollectionExtensions
{
    public static IServiceCollection AddKyvoClient(this IServiceCollection services, Action<KyvoClientOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddHttpClient<IKyvoProductClient, KyvoProductClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KyvoClientOptions>>().Value;
                client.BaseAddress = new Uri(options.Authority.TrimEnd('/') + "/");
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KyvoClientOptions>>().Value;
                return DevKyvoCertificateHandler.Create(options.AllowInvalidCertificate);
            });

        return services;
    }

    public static IServiceCollection AddKyvoClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = KyvoClientOptions.SectionName)
    {
        services.Configure<KyvoClientOptions>(configuration.GetSection(sectionName));
        return services.AddKyvoClient();
    }

    /// <summary>
    /// Bearer token from the current request (platform or tenant).
    /// </summary>
    public static string? GetUserAccessToken(this IHttpContextAccessor accessor) =>
        accessor.GetBearerToken();

    /// <summary>
    /// Platform OIDC access token from the Authorization header.
    /// </summary>
    public static string? GetPlatformAccessToken(this IHttpContextAccessor accessor)
    {
        var token = accessor.GetBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenUse = KyvoTokenParser.TryGetTokenUse(token);
        return string.Equals(tokenUse, "tenant", StringComparison.OrdinalIgnoreCase) ? null : token;
    }

    /// <summary>
    /// Tenant-scoped JWT (<c>token_use=tenant</c>) from the Authorization header.
    /// </summary>
    public static string? GetTenantAccessToken(this IHttpContextAccessor accessor)
    {
        var token = accessor.GetBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return string.Equals(KyvoTokenParser.TryGetTokenUse(token), "tenant", StringComparison.OrdinalIgnoreCase)
            ? token
            : null;
    }

    private static string? GetBearerToken(this IHttpContextAccessor accessor)
    {
        var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header)
            || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return header["Bearer ".Length..].Trim();
    }
}
