using Kyvo.Application.Interfaces;
using Kyvo.Application.Ports.Email;
using Kyvo.Application.Services.Security;
using Kyvo.Application.Services.TenantResolutionCache;
using Kyvo.Application.Services.TenantRoles;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.Services.UserScope;
using Kyvo.Infrastructure.Services.Email;
using Kyvo.Infrastructure.Services.Invite;
using Kyvo.Application.Ports.Identity;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Infrastructure.Services.Identity;
using Kyvo.Infrastructure.Services.Oidc;
using Kyvo.Infrastructure.Services.Security;
using Kyvo.Infrastructure.Services.TenantResolutionCache;
using Kyvo.Infrastructure.Services.TenantRoles;
using Kyvo.Infrastructure.Services.UnitOfWork;
using Kyvo.Infrastructure.Services.UserScope;
using Kyvo.Application.Ports.Platform;
using Kyvo.Infrastructure.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Kyvo.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPlatformBootstrapExecutor, PlatformBootstrapExecutor>();
        services.AddScoped<IUserScope, HttpUserScope>();
        services.AddScoped<IInviteTokenHasher, InviteTokenHasher>();
        services.AddScoped<ITenantRoleResolver, TenantRoleResolver>();
        services.AddScoped<ITenantResolutionCache, DistributedTenantResolutionCache>();
        services.AddScoped<IEmailService, AwsSesEmailService>();
        services.AddSingleton<IInviteTokenProtector, InviteTokenProtector>();
        services.AddScoped<IInvitePolicy, InvitePolicy>();
        services.AddScoped<IKyvoClaimsPrincipalFactory, KyvoClaimsPrincipalFactory>();
        services.AddScoped<IUserAccountService, UserAccountService>();

        return services;
    }
}
