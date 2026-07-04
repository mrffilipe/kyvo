using Kyvo.Application.Policies;
using Kyvo.Application.Ports.Branding;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Services.AccountBranding;
using Kyvo.Application.Queries.Applications.CheckApplicationSlugAvailability;
using Kyvo.Application.Queries.Applications.GetApplicationBranding;
using Kyvo.Application.Queries.Applications.GetApplicationById;
using Kyvo.Application.Queries.Applications.ListApplications;
using Kyvo.Application.Queries.AuditLogs.ListAuditLogFilterOptions;
using Kyvo.Application.Queries.AuditLogs.ListAuditLogs;
using Kyvo.Application.Queries.Auth.ListActiveSessions;
using Kyvo.Application.Queries.IdentityProviders.CheckIdentityProviderAliasAvailability;
using Kyvo.Application.Queries.IdentityProviders.GetIdentityProviderById;
using Kyvo.Application.Queries.IdentityProviders.ListIdentityProviders;
using Kyvo.Application.Queries.Invites.ListInvitesByTenant;
using Kyvo.Application.Queries.Memberships.ListMembershipsByTenant;
using Kyvo.Application.Queries.Platform.GetPlatformStatus;
using Kyvo.Application.Queries.TenantRoles.ListTenantRoles;
using Kyvo.Application.Queries.Tenants.CheckTenantKeyAvailability;
using Kyvo.Application.Queries.Tenants.GetTenantById;
using Kyvo.Application.Queries.Tenants.ListTenantsByUser;
using Kyvo.Application.Queries.Users.GetUserById;
using Kyvo.Application.Queries.Users.ListUserMemberships;
using Kyvo.Application.Queries.Users.SearchUsers;
using Kyvo.Application.Shared;
using Kyvo.Application.UseCases.Applications.CreateApplication;
using Kyvo.Application.UseCases.Applications.CreateApplicationClient;
using Kyvo.Application.UseCases.Applications.DeleteApplicationBrandingLogo;
using Kyvo.Application.UseCases.Applications.ProvisionTenant;
using Kyvo.Application.UseCases.Applications.UpdateApplicationBranding;
using Kyvo.Application.UseCases.Applications.UploadApplicationBrandingLogo;
using Kyvo.Application.UseCases.Auth.DeleteAccount;
using Kyvo.Application.UseCases.Auth.DeleteTenant;
using Kyvo.Application.UseCases.Auth.ExternalLogin;
using Kyvo.Application.UseCases.Auth.RevokeSession;
using Kyvo.Application.UseCases.Auth.SubscribeTenant;
using Kyvo.Application.UseCases.Auth.SwitchTenant;
using Kyvo.Application.UseCases.IdentityProviders.AddIdentityProvider;
using Kyvo.Application.UseCases.IdentityProviders.DisableIdentityProvider;
using Kyvo.Application.UseCases.IdentityProviders.EnableIdentityProvider;
using Kyvo.Application.UseCases.IdentityProviders.UpdateIdentityProvider;
using Kyvo.Application.UseCases.Invites.AcceptInvite;
using Kyvo.Application.UseCases.Invites.InviteMember;
using Kyvo.Application.UseCases.Invites.RevokeInvite;
using Kyvo.Application.UseCases.Memberships.CreateMembership;
using Kyvo.Application.UseCases.Memberships.RevokeMembership;
using Kyvo.Application.UseCases.Memberships.UpdateMembershipRoles;
using Kyvo.Application.UseCases.Platform.BootstrapPlatform;
using Kyvo.Application.UseCases.TenantRoles.CreateTenantRole;
using Kyvo.Application.UseCases.TenantRoles.DeleteTenantRole;
using Kyvo.Application.UseCases.TenantRoles.UpdateTenantRole;
using Kyvo.Application.UseCases.Tenants.CreateTenant;
using Kyvo.Application.UseCases.Tenants.UpdateTenant;
using Kyvo.Application.UseCases.Users.UpdateUserProfile;
using Kyvo.Application.Ports.Tenants;
using Kyvo.Infrastructure.Policies;
using Kyvo.Infrastructure.Services.AccountBranding;
using Kyvo.Infrastructure.Services.AppService;
using Kyvo.Infrastructure.Services.Oidc;
using Kyvo.Infrastructure.Services.Tenant;
using Kyvo.Infrastructure.Queries.Applications;
using Kyvo.Infrastructure.Queries.AuditLogs;
using Kyvo.Infrastructure.Queries.Auth;
using Kyvo.Infrastructure.Queries.IdentityProviders;
using Kyvo.Infrastructure.Queries.Invites;
using Kyvo.Infrastructure.Queries.Memberships;
using Kyvo.Infrastructure.Queries.Platform;
using Kyvo.Infrastructure.Queries.TenantRoles;
using Kyvo.Infrastructure.Queries.Tenants;
using Kyvo.Infrastructure.Queries.Users;
using Kyvo.Infrastructure.Shared;
using Kyvo.Application.UseCases.Applications;
using Kyvo.Application.UseCases.Auth;
using Kyvo.Application.UseCases.IdentityProviders;
using Microsoft.Extensions.DependencyInjection;

namespace Kyvo.Infrastructure.Extensions;

public static class UseCaseExtensions
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<ITenantProvisioner, TenantProvisioner>();
        services.AddScoped<ITenantAuthorizationPolicy, TenantAuthorizationPolicy>();
        services.AddScoped<ITenantAccountEligibilityPolicy, TenantAccountEligibilityPolicy>();
        services.AddScoped<ITenantCascadeDeleter, TenantCascadeDeleter>();

        services.AddScoped<ICreateTenantUseCase, CreateTenantUseCase>();
        services.AddScoped<IUpdateTenantUseCase, UpdateTenantUseCase>();
        services.AddScoped<ISubscribeTenantUseCase, SubscribeTenantUseCase>();
        services.AddScoped<IProvisionTenantUseCase, ProvisionTenantUseCase>();

        services.AddScoped<IInviteMemberUseCase, InviteMemberUseCase>();
        services.AddScoped<IAcceptInviteUseCase, AcceptInviteUseCase>();
        services.AddScoped<IRevokeInviteUseCase, RevokeInviteUseCase>();

        services.AddScoped<ISwitchTenantUseCase, SwitchTenantUseCase>();
        services.AddScoped<IDeleteAccountUseCase, DeleteAccountUseCase>();
        services.AddScoped<IDeleteTenantUseCase, DeleteTenantUseCase>();
        services.AddScoped<IRevokeSessionUseCase, RevokeSessionUseCase>();

        services.AddScoped<IExternalLoginUseCase, ExternalLoginUseCase>();
        services.AddScoped<IBootstrapPlatformUseCase, BootstrapPlatformUseCase>();

        services.AddScoped<ICreateApplicationUseCase, CreateApplicationUseCase>();
        services.AddScoped<ICreateApplicationClientUseCase, CreateApplicationClientUseCase>();
        services.AddScoped<IUpdateApplicationBrandingUseCase, UpdateApplicationBrandingUseCase>();
        services.AddScoped<IUploadApplicationBrandingLogoUseCase, UploadApplicationBrandingLogoUseCase>();
        services.AddScoped<IDeleteApplicationBrandingLogoUseCase, DeleteApplicationBrandingLogoUseCase>();

        services.AddScoped<ICreateMembershipUseCase, CreateMembershipUseCase>();
        services.AddScoped<IUpdateMembershipRolesUseCase, UpdateMembershipRolesUseCase>();
        services.AddScoped<IRevokeMembershipUseCase, RevokeMembershipUseCase>();

        services.AddScoped<ICreateTenantRoleUseCase, CreateTenantRoleUseCase>();
        services.AddScoped<IUpdateTenantRoleUseCase, UpdateTenantRoleUseCase>();
        services.AddScoped<IDeleteTenantRoleUseCase, DeleteTenantRoleUseCase>();

        services.AddScoped<IAddIdentityProviderUseCase, AddIdentityProviderUseCase>();
        services.AddScoped<IUpdateIdentityProviderUseCase, UpdateIdentityProviderUseCase>();
        services.AddScoped<IEnableIdentityProviderUseCase, EnableIdentityProviderUseCase>();
        services.AddScoped<IDisableIdentityProviderUseCase, DisableIdentityProviderUseCase>();

        services.AddScoped<IUpdateUserProfileUseCase, UpdateUserProfileUseCase>();

        services.AddScoped<IApplicationBrandingStorage, ApplicationBrandingStorage>();
        services.AddScoped<IAccountBrandingResolver, AccountBrandingResolver>();
        services.AddScoped<IOpenIddictApplicationSyncService, OpenIddictApplicationSyncService>();

        return services;
    }

    public static IServiceCollection AddQueries(this IServiceCollection services)
    {
        services.AddScoped<IListInvitesByTenantQuery, ListInvitesByTenantQuery>();
        services.AddScoped<IListActiveSessionsQuery, ListActiveSessionsQuery>();
        services.AddScoped<IGetPlatformStatusQuery, GetPlatformStatusQuery>();

        services.AddScoped<IGetTenantByIdQuery, GetTenantByIdQuery>();
        services.AddScoped<IListTenantsByUserQuery, ListTenantsByUserQuery>();
        services.AddScoped<ICheckTenantKeyAvailabilityQuery, CheckTenantKeyAvailabilityQuery>();

        services.AddScoped<ICheckApplicationSlugAvailabilityQuery, CheckApplicationSlugAvailabilityQuery>();
        services.AddScoped<IGetApplicationByIdQuery, GetApplicationByIdQuery>();
        services.AddScoped<IListApplicationsQuery, ListApplicationsQuery>();
        services.AddScoped<IGetApplicationBrandingQuery, GetApplicationBrandingQuery>();

        services.AddScoped<IListMembershipsByTenantQuery, ListMembershipsByTenantQuery>();
        services.AddScoped<IListTenantRolesQuery, ListTenantRolesQuery>();

        services.AddScoped<ICheckIdentityProviderAliasAvailabilityQuery, CheckIdentityProviderAliasAvailabilityQuery>();
        services.AddScoped<IGetIdentityProviderByIdQuery, GetIdentityProviderByIdQuery>();
        services.AddScoped<IListIdentityProvidersQuery, ListIdentityProvidersQuery>();

        services.AddScoped<IGetUserByIdQuery, GetUserByIdQuery>();
        services.AddScoped<IListUserMembershipsQuery, ListUserMembershipsQuery>();
        services.AddScoped<ISearchUsersQuery, SearchUsersQuery>();

        services.AddScoped<IListAuditLogsQuery, ListAuditLogsQuery>();
        services.AddScoped<IListAuditLogFilterOptionsQuery, ListAuditLogFilterOptionsQuery>();

        return services;
    }
}
