using System.Security.Claims;
using Kyvo.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kyvo.API.Tests;

public sealed class AuthorizationPolicyTests
{
    [Fact]
    public async Task RequireTenantToken_SucceedsWithTenantClaims()
    {
        var auth = BuildAuthorizationService();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("token_use", "tenant"),
            new Claim("tid", Guid.NewGuid().ToString("D")),
        ], authenticationType: "Test"));

        var result = await auth.AuthorizeAsync(user, "RequireTenantToken");
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task RequireTenantToken_FailsWithPlatformTokenOnly()
    {
        var auth = BuildAuthorizationService();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString("D")),
            new Claim("sid", Guid.NewGuid().ToString("D")),
        ], authenticationType: "Test"));

        var result = await auth.AuthorizeAsync(user, "RequireTenantToken");
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task TenantOwnerOrAdmin_SucceedsForOwnerRole()
    {
        var auth = BuildAuthorizationService();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("token_use", "tenant"),
            new Claim("tid", Guid.NewGuid().ToString("D")),
            new Claim("trole", TenantRoleDefaults.OWNER),
        ], authenticationType: "Test"));

        var result = await auth.AuthorizeAsync(user, "TenantOwnerOrAdmin");
        Assert.True(result.Succeeded);
    }

    private static IAuthorizationService BuildAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
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

        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }
}
