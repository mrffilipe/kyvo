using Kyvo.Domain.Entities;
using Kyvo.Infrastructure.Common;
using Kyvo.Infrastructure.Configurations;
using Kyvo.Infrastructure.Identity;
using Kyvo.Infrastructure.Persistence;
using Kyvo.Infrastructure.Services.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kyvo.Infrastructure.Extensions;

public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Configures ASP.NET Core Identity (user store, password hashing, lockout, external logins) as the
    /// single source of truth for authentication.
    /// </summary>
    public static IServiceCollection AddKyvoIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        var passwordPolicy = configuration.GetSection(PasswordPolicyOptions.SECTION).Get<PasswordPolicyOptions>()
            ?? throw new InvalidOperationException(InfrastructureErrorMessages.PasswordPolicy.MIN_LENGTH_REQUIRED);

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = passwordPolicy.MinLength;
                options.Password.RequireDigit = passwordPolicy.RequireDigit;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedAccount = false;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddPasswordValidator<KyvoPasswordValidator>()
            .AddSignInManager()
            .AddClaimsPrincipalFactory<KyvoUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            .AddCookie(IdentityConstants.ExternalScheme, options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            });

        return services;
    }
}
