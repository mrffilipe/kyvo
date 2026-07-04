using Kyvo.Application.Services.IdentityProvider;
using Kyvo.Infrastructure.Services.IdentityProvider;
using Microsoft.Extensions.DependencyInjection;

namespace Kyvo.Infrastructure.Extensions;

public static class IdentityProviderFederationExtensions
{
    public static IServiceCollection AddIdentityProviderFederation(this IServiceCollection services)
    {
        services.AddScoped<IIdentityProviderConfigValidator, IdentityProviderConfigValidator>();

        return services;
    }
}
