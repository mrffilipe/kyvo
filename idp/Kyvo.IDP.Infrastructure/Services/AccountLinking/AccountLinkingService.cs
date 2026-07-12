using Kyvo.IDP.Application.Services.AccountLinking;
using Kyvo.IDP.Application.Services.Claims;
using Kyvo.IDP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Kyvo.IDP.Infrastructure.Services.AccountLinking;

public sealed class AccountLinkingService : IAccountLinkingService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountLinkingService> _logger;

    public AccountLinkingService(UserManager<ApplicationUser> userManager, ILogger<AccountLinkingService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<AccountLinkResult> LinkOrCreateAsync(MappedExternalIdentity identity, CancellationToken ct = default)
    {
        var existingByLogin = await _userManager.FindByLoginAsync(identity.Provider, identity.ProviderUserId);
        if (existingByLogin is not null)
        {
            _logger.LogInformation(
                "Federated login reused existing linked account {UserId} via {Provider}",
                existingByLogin.Id,
                identity.Provider);

            return ToResult(existingByLogin, created: false, linked: false);
        }

        ApplicationUser? user = null;
        var linked = false;
        var created = false;

        if (identity.EmailVerified)
        {
            user = await _userManager.FindByEmailAsync(identity.Email);
            if (user is not null)
            {
                var addLogin = await _userManager.AddLoginAsync(
                    user,
                    new UserLoginInfo(identity.Provider, identity.ProviderUserId, identity.Provider));

                if (!addLogin.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to link {identity.Provider}: {string.Join("; ", addLogin.Errors.Select(e => e.Description))}");
                }

                linked = true;
                UpdateProfileFromExternal(user, identity);
                await _userManager.UpdateAsync(user);

                _logger.LogInformation(
                    "Linked {Provider} to existing account {UserId} by verified email",
                    identity.Provider,
                    user.Id);
            }
        }

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = identity.Email,
                Email = identity.Email,
                EmailConfirmed = identity.EmailVerified,
                DisplayName = BuildDisplayName(identity),
                PhotoUrl = identity.Picture,
                IsActive = true
            };
            user.SetCreatedAt();

            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create user: {string.Join("; ", create.Errors.Select(e => e.Description))}");
            }

            var addLogin = await _userManager.AddLoginAsync(
                user,
                new UserLoginInfo(identity.Provider, identity.ProviderUserId, identity.Provider));

            if (!addLogin.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to attach login: {string.Join("; ", addLogin.Errors.Select(e => e.Description))}");
            }

            created = true;
            linked = true;

            _logger.LogInformation(
                "Created Kyvo account {UserId} from {Provider} federated login",
                user.Id,
                identity.Provider);
        }

        return ToResult(user, created, linked);
    }

    private static void UpdateProfileFromExternal(ApplicationUser user, MappedExternalIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(user.DisplayName))
        {
            user.DisplayName = BuildDisplayName(identity);
        }

        if (string.IsNullOrWhiteSpace(user.PhotoUrl) && !string.IsNullOrWhiteSpace(identity.Picture))
        {
            user.PhotoUrl = identity.Picture;
        }

        if (identity.EmailVerified && !user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
        }

        user.SetUpdatedAt();
    }

    private static string BuildDisplayName(MappedExternalIdentity identity)
    {
        if (!string.IsNullOrWhiteSpace(identity.Name))
        {
            return identity.Name.Trim();
        }

        var composed = $"{identity.GivenName} {identity.FamilyName}".Trim();
        return string.IsNullOrWhiteSpace(composed) ? identity.Email : composed;
    }

    private static AccountLinkResult ToResult(ApplicationUser user, bool created, bool linked) => new()
    {
        UserId = user.Id,
        Email = user.Email!,
        DisplayName = user.DisplayName,
        Created = created,
        Linked = linked
    };
}
