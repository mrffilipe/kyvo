using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Identity;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Ports.Platform;
using Kyvo.Application.Queries.Platform.GetPlatformStatus;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;
using Kyvo.Application.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kyvo.Application.UseCases.Platform.BootstrapPlatform;

public sealed class BootstrapPlatformUseCase : IBootstrapPlatformUseCase
{
    private readonly IGetPlatformStatusQuery _platformStatusQuery;
    private readonly IPlatformConfigurationRepository _platformConfigurations;
    private readonly IUserAccountService _userAccounts;
    private readonly IUserPlatformRoleRepository _userPlatformRoles;
    private readonly IPlatformRoleRepository _platformRoles;
    private readonly IIdentityProviderRepository _identityProviders;
    private readonly IApplicationRepository _applications;
    private readonly IApplicationClientRepository _clients;
    private readonly IOpenIddictApplicationSyncService _openIddictSync;
    private readonly IPlatformBootstrapExecutor _bootstrapExecutor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly BootstrapOptions _bootstrapOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<BootstrapPlatformUseCase> _logger;

    public BootstrapPlatformUseCase(
        IGetPlatformStatusQuery platformStatusQuery,
        IPlatformConfigurationRepository platformConfigurations,
        IUserAccountService userAccounts,
        IUserPlatformRoleRepository userPlatformRoles,
        IPlatformRoleRepository platformRoles,
        IIdentityProviderRepository identityProviders,
        IApplicationRepository applications,
        IApplicationClientRepository clients,
        IOpenIddictApplicationSyncService openIddictSync,
        IPlatformBootstrapExecutor bootstrapExecutor,
        IUnitOfWork unitOfWork,
        IOptions<BootstrapOptions> bootstrapOptions,
        IOptions<JwtOptions> jwtOptions,
        ILogger<BootstrapPlatformUseCase> logger)
    {
        _platformStatusQuery = platformStatusQuery;
        _platformConfigurations = platformConfigurations;
        _userAccounts = userAccounts;
        _userPlatformRoles = userPlatformRoles;
        _platformRoles = platformRoles;
        _identityProviders = identityProviders;
        _applications = applications;
        _clients = clients;
        _openIddictSync = openIddictSync;
        _bootstrapExecutor = bootstrapExecutor;
        _unitOfWork = unitOfWork;
        _bootstrapOptions = bootstrapOptions.Value;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var status = await _platformStatusQuery.ExecuteAsync(ct);
        if (!status.RequiresBootstrap)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_bootstrapOptions.AdminEmail) ||
            string.IsNullOrWhiteSpace(_bootstrapOptions.AdminPassword))
        {
            _logger.LogWarning(
                "Platform not initialized. Configure Bootstrap:AdminEmail and Bootstrap:AdminPassword, then restart.");
            return;
        }

        try
        {
            await InitializePlatformAsync(ct);
            _logger.LogInformation("Platform initialized (root user and OAuth client created).");
        }
        catch (DomainBusinessRuleException ex) when (
            ex.Message == ApplicationErrorMessages.Auth.PLATFORM_BOOTSTRAP_ALREADY_COMPLETED)
        {
            // Another replica completed initialization concurrently.
        }
    }

    private async Task InitializePlatformAsync(CancellationToken ct = default)
    {
        var adminEmail = _bootstrapOptions.AdminEmail;
        var adminPassword = _bootstrapOptions.AdminPassword;

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new DomainBusinessRuleException(
                ApplicationErrorMessages.Auth.PLATFORM_BOOTSTRAP_ADMIN_CREDENTIALS_NOT_CONFIGURED);
        }

        await _bootstrapExecutor.ExecuteAsync(async transactionCt =>
        {
            var configuration = await _platformConfigurations.GetForUpdateAsync(transactionCt);
            if (configuration?.IsBootstrapped == true)
            {
                throw new DomainBusinessRuleException(
                    ApplicationErrorMessages.Auth.PLATFORM_BOOTSTRAP_ALREADY_COMPLETED);
            }

            if (await _applications.SlugAlreadyExistsAsync(
                PlatformDefaults.AdminConsole.APPLICATION_SLUG,
                transactionCt))
            {
                throw new DomainBusinessRuleException(
                    ApplicationErrorMessages.Auth.PLATFORM_BOOTSTRAP_APPLICATION_SLUG_ALREADY_EXISTS);
            }

            if (await _clients.GetByClientIdAsync(PlatformDefaults.AdminConsole.CLIENT_ID, transactionCt) is not null)
            {
                throw new DomainBusinessRuleException(
                    ApplicationErrorMessages.Auth.PLATFORM_BOOTSTRAP_CLIENT_ID_ALREADY_EXISTS);
            }

            var user = await _userAccounts.FindByEmailAsync(adminEmail.Trim(), transactionCt);
            if (user is null)
            {
                var displayName = string.IsNullOrWhiteSpace(_bootstrapOptions.AdminDisplayName)
                    ? adminEmail.Split('@')[0]
                    : _bootstrapOptions.AdminDisplayName.Trim();

                user = new User(new EmailAddress(adminEmail.Trim()), displayName);
                var createResult = await _userAccounts.CreateWithPasswordAsync(user, adminPassword, transactionCt);
                if (!createResult.Succeeded)
                {
                    throw new DomainBusinessRuleException(
                        string.Join(" ", createResult.Errors));
                }
            }
            else if (!await _userAccounts.HasPasswordAsync(user.Id, transactionCt))
            {
                var addPasswordResult = await _userAccounts.AddPasswordAsync(user.Id, adminPassword, transactionCt);
                if (!addPasswordResult.Succeeded)
                {
                    throw new DomainBusinessRuleException(
                        string.Join(" ", addPasswordResult.Errors));
                }
            }

            var platAdminRole = await _platformRoles.GetByKeyAsync(
                PlatformRoleDefaults.PLATFORM_ADMINISTRATOR,
                transactionCt);

            if (platAdminRole is null)
            {
                platAdminRole = new PlatformRole(
                    PlatformRoleDefaults.PLATFORM_ADMINISTRATOR,
                    "Platform Administrator",
                    isSystem: true);
                await _platformRoles.AddAsync(platAdminRole, transactionCt);
            }

            if (!await _userPlatformRoles.AssignmentAlreadyExistsAsync(user.Id, platAdminRole.Id, transactionCt))
            {
                await _userPlatformRoles.AddAsync(
                    new UserPlatformRole(user.Id, platAdminRole.Id),
                    transactionCt);
            }

            var localIdp = await _identityProviders.GetByAliasAsync(
                PlatformDefaults.LocalIdentityProvider.ALIAS,
                transactionCt);

            if (localIdp is null)
            {
                localIdp = new Domain.Entities.IdentityProvider(
                    PlatformDefaults.LocalIdentityProvider.ALIAS,
                    PlatformDefaults.LocalIdentityProvider.DISPLAY_NAME,
                    IdentityProviderType.Local,
                    new[] { IdpCapability.LocalPassword },
                    enabled: true);
                await _identityProviders.AddAsync(localIdp, transactionCt);
            }

            var application = new Domain.Entities.Application(
                PlatformDefaults.AdminConsole.APPLICATION_NAME,
                PlatformDefaults.AdminConsole.APPLICATION_SLUG,
                ApplicationType.Web,
                isSystem: true);
            await _applications.AddAsync(application, transactionCt);

            var client = new ApplicationClient(
                application.Id,
                PlatformDefaults.AdminConsole.CLIENT_ID,
                ClientType.Public,
                BuildAdminConsoleRedirectUris().ToList(),
                PlatformDefaults.AdminConsole.AllowedScopes.ToList(),
                accessTokenTtlSeconds: 900,
                BuildAdminConsolePostLogoutRedirectUris().ToList(),
                isSystem: true);
            await _clients.AddAsync(client, transactionCt);

            if (configuration is null)
            {
                configuration = new PlatformConfiguration();
                await _platformConfigurations.AddAsync(configuration, transactionCt);
            }

            configuration.MarkBootstrapped(user.Id, client.ClientId);

            await _unitOfWork.SaveChangesAsync(transactionCt);
            await _openIddictSync.SyncAsync(client, plainTextClientSecret: null, transactionCt);
        }, ct);
    }

    private IReadOnlyList<string> BuildAdminConsoleRedirectUris()
    {
        var uris = new HashSet<string>(StringComparer.Ordinal);
        foreach (var uri in PlatformDefaults.AdminConsole.DefaultRedirectUris)
        {
            uris.Add(uri);
        }

        var issuer = _jwtOptions.Issuer.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(issuer))
        {
            uris.Add($"{issuer}/auth/callback");
        }

        return uris.ToList();
    }

    private IReadOnlyList<string> BuildAdminConsolePostLogoutRedirectUris()
    {
        var uris = new HashSet<string>(StringComparer.Ordinal);
        foreach (var uri in PlatformDefaults.AdminConsole.DefaultPostLogoutRedirectUris)
        {
            uris.Add(uri);
        }

        var issuer = _jwtOptions.Issuer.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(issuer))
        {
            uris.Add($"{issuer}/login");
        }

        return uris.ToList();
    }
}
