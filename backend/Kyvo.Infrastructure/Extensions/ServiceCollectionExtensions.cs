using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Kyvo.Application.Services.IdentityProvider;
using Kyvo.Infrastructure.Services.IdentityProvider;
using Kyvo.Application.Services.Security;
using Kyvo.Application.Configurations;
using Kyvo.Infrastructure.Configurations;
using Kyvo.Infrastructure.Persistence;
using Kyvo.Infrastructure.Persistence.Interceptors;
using Kyvo.Infrastructure.Services.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Kyvo.Infrastructure.Common;

namespace Kyvo.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SECTION))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SECTION))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SECTION))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();

        services.AddOptions<InviteOptions>()
            .Bind(configuration.GetSection(InviteOptions.SECTION))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<InviteOptions>, InviteOptionsValidator>();

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SECTION))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();

        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SECTION))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RedisOptions>, RedisOptionsValidator>();

        services.AddOptions<BootstrapOptions>()
            .Bind(configuration.GetSection(BootstrapOptions.SECTION))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<BootstrapOptions>, BootstrapOptionsValidator>();

        services.AddOptions<SecretProtectionOptions>()
            .Bind(configuration.GetSection(SecretProtectionOptions.SECTION))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SecretProtectionOptions>, SecretProtectionOptionsValidator>();

        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection(PasswordPolicyOptions.SECTION))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<PasswordPolicyOptions>, PasswordPolicyOptionsValidator>();

        services.AddSecretProtection(configuration);

        services.AddHttpContextAccessor();
        services.AddDistributedCaching(configuration);
        services.AddSingleton<IAmazonSimpleEmailServiceV2>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;
            var region = RegionEndpoint.GetBySystemName(options.Region);

            if (!string.IsNullOrWhiteSpace(options.AccessKeyId) &&
                !string.IsNullOrWhiteSpace(options.SecretAccessKey))
            {
                if (!string.IsNullOrWhiteSpace(options.SessionToken))
                {
                    return new AmazonSimpleEmailServiceV2Client(
                        new SessionAWSCredentials(
                            options.AccessKeyId,
                            options.SecretAccessKey,
                            options.SessionToken),
                        region);
                }

                return new AmazonSimpleEmailServiceV2Client(
                    new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey),
                    region);
            }

            return new AmazonSimpleEmailServiceV2Client(region);
        });

        services.AddScoped<TenantStore>();
        services.AddDbContext(configuration);
        services.AddRepositories();
        services.AddServices();
        services.AddUseCases();
        services.AddQueries();
        services.AddIdentityProviderFederation();

        return services;
    }

    private static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(DatabaseOptions.SECTION)["ConnectionString"];
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>());
            // Registers the OpenIddictApplications/Authorizations/Scopes/Tokens entity sets with a Guid key,
            // matching Kyvo's own primary key convention.
            options.UseOpenIddict<Guid>();
        });
        return services;
    }

    private static IServiceCollection AddDistributedCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSection = configuration.GetSection(RedisOptions.SECTION);
        var connectionString = redisSection[nameof(RedisOptions.ConnectionString)];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var instanceName = redisSection[nameof(RedisOptions.InstanceName)]!;
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = instanceName;
            });
            return services;
        }

        services.AddDistributedMemoryCache();
        return services;
    }

    private static IServiceCollection AddSecretProtection(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(SecretProtectionOptions.SECTION).Get<SecretProtectionOptions>()
                      ?? throw new InvalidOperationException(
                          InfrastructureErrorMessages.SecretProtection.KEY_DIRECTORY_PATH_REQUIRED);

        var keyDirectory = Path.IsPathRooted(options.KeyDirectoryPath)
            ? options.KeyDirectoryPath
            : Path.Combine(AppContext.BaseDirectory, options.KeyDirectoryPath);
        Directory.CreateDirectory(keyDirectory);

        services.AddDataProtection()
            .SetApplicationName(options.ApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));

        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.AddSingleton<IIdentityProviderConfigCipher, IdentityProviderConfigCipher>();

        return services;
    }
}
