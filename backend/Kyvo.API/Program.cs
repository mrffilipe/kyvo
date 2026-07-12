using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Kyvo.API.Components;
using Kyvo.API.Middleware;
using Kyvo.API.Middlewares;
using Kyvo.API.Services;
using Kyvo.API.Swagger;
using Kyvo.Application.Configurations;
using Kyvo.Application.Services.AccountBranding;
using Kyvo.Application.Services.IdentityProvider;
using Kyvo.Application.UseCases.Platform.BootstrapPlatform;
using Kyvo.Domain.Constants;
using Kyvo.Infrastructure.Configurations;
using Kyvo.Infrastructure.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.AddApiDocumentation();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddRazorComponents();
    builder.Services.AddScoped<IAccountLoginPageStateProvider, AccountLoginPageStateProvider>();
    builder.Services.AddScoped<IAccountBrandingStateProvider, AccountBrandingStateProvider>();

    builder.Services
        .AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddKyvoIdentity(builder.Configuration);
    builder.Services.AddKyvoOpenIddictServer(builder.Configuration);
    builder.Services.AddKyvoOpenIddictClient();
    builder.Services.AddScoped<IAccountSignInService, AccountSignInService>();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("PlatformAdministrator", policy =>
            policy.RequireClaim(PlatformRoleDefaults.CLAIM_TYPE, PlatformRoleDefaults.PLATFORM_ADMINISTRATOR));

        options.AddPolicy("RequireTenantToken", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("token_use", "tenant");
            policy.RequireClaim("tid");
        });

        options.AddPolicy("TenantOwnerOrAdmin", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("token_use", "tenant");
            policy.RequireAssertion(context =>
            {
                var roles = context.User.FindAll("trole").Select(c => c.Value);
                return roles.Any(r => string.Equals(r, TenantRoleDefaults.OWNER, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(r, TenantRoleDefaults.ADMIN, StringComparison.OrdinalIgnoreCase));
            });
        });
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("account_register", context =>
        {
            if (!HttpMethods.IsPost(context.Request.Method))
            {
                return RateLimitPartition.GetNoLimiter("account_register_get");
            }

            var rateLimitOptions = context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
            return RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.AccountRegisterPermitLimit,
                    Window = TimeSpan.FromMinutes(rateLimitOptions.AccountRegisterWindowMinutes),
                    QueueLimit = 0
                });
        });

        options.AddPolicy("account_signin", context =>
        {
            if (!HttpMethods.IsPost(context.Request.Method))
            {
                return RateLimitPartition.GetNoLimiter("account_signin_get");
            }

            var rateLimitOptions = context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
            return RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.AccountRegisterPermitLimit,
                    Window = TimeSpan.FromMinutes(rateLimitOptions.AccountRegisterWindowMinutes),
                    QueueLimit = 0
                });
        });

        options.AddPolicy("connect_token", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
    });

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddAntiforgery(options =>
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<Kyvo.Infrastructure.Persistence.ApplicationDbContext>("database");

    var app = builder.Build();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IBootstrapPlatformUseCase>().ExecuteAsync();
    }

    app.UseSerilogRequestLogging();
    app.UseForwardedHeaders();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<ApplicationExceptionMiddleware>();

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "REST API v1");
            options.SwaggerEndpoint("/swagger/oidc/swagger.json", "OAuth / OIDC / Account");
        });
    }

    app.UseCors("AllowAll");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseMiddleware<TenantContextMiddleware>();
    app.UseAuthorization();
    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapHealthChecks("/health");
    app.MapControllers();
    app.MapRazorComponents<App>();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Kyvo IDP terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
