using Kyvo.Application.Common;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.AppService;
using Kyvo.Application.Services.Tenant;
using Kyvo.Application.Services.TenantResolutionCache;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Services.AppService;

public sealed class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _applications;
    private readonly IApplicationClientRepository _clients;
    private readonly IApplicationTenantRepository _applicationTenants;
    private readonly ITenantRepository _tenants;
    private readonly ITenantRoleRepository _roles;
    private readonly ITenantMembershipRepository _memberships;
    private readonly IUserRepository _users;
    private readonly ITenantResolutionCache _tenantResolutionCache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly IApplicationBrandingStorage _brandingStorage;
    private readonly ITenantService _tenantService;

    public ApplicationService(
        IApplicationRepository applications,
        IApplicationClientRepository clients,
        IApplicationTenantRepository applicationTenants,
        ITenantRepository tenants,
        ITenantRoleRepository roles,
        ITenantMembershipRepository memberships,
        IUserRepository users,
        ITenantResolutionCache tenantResolutionCache,
        IUnitOfWork unitOfWork,
        ApplicationDbContext context,
        IApplicationBrandingStorage brandingStorage,
        ITenantService tenantService)
    {
        _applications = applications;
        _clients = clients;
        _applicationTenants = applicationTenants;
        _tenants = tenants;
        _roles = roles;
        _memberships = memberships;
        _users = users;
        _tenantResolutionCache = tenantResolutionCache;
        _unitOfWork = unitOfWork;
        _context = context;
        _brandingStorage = brandingStorage;
        _tenantService = tenantService;
    }

    public async Task<Guid> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await _applications.SlugAlreadyExistsAsync(slug, cancellationToken))
        {
            throw new DomainBusinessRuleException(ApplicationErrorMessages.Application.SlugAlreadyExists);
        }

        var application = new Domain.Entities.Application(request.Name, slug, request.Type);
        await _applications.AddAsync(application, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return application.Id;
    }

    public async Task<AvailabilityDto> IsSlugAvailableAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return new AvailabilityDto { Available = false };
        }

        var normalized = slug.Trim().ToLowerInvariant();
        var exists = await _applications.SlugAlreadyExistsAsync(normalized, cancellationToken);
        return new AvailabilityDto { Available = !exists };
    }

    public async Task<Guid> CreateClientAsync(
        CreateApplicationClientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.ActorPlatformRoles.Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role)))
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.UserHasNoTenantAccess);
        }

        var client = new ApplicationClient(
            request.ApplicationId,
            request.ClientId,
            request.ClientSecretHash,
            request.ClientType,
            ApplicationClientListFields.ParseAndValidateRedirectUris(request.RedirectUris),
            ApplicationClientListFields.ParseAndValidateAllowedScopes(request.AllowedScopes, request.AllowedScopesList),
            request.AccessTokenTtlSeconds);

        await _clients.AddAsync(client, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return client.Id;
    }

    public async Task<ProvisionApplicationTenantResult> ProvisionTenantAsync(
        ProvisionApplicationTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.ActorPlatformRoles.Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role)))
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.UserHasNoTenantAccess);
        }

        var application = await _applications.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.Application.NotFound);

        var tenantKey = new TenantKey(request.TenantKey);
        if (await _tenants.KeyAlreadyExistsAsync(tenantKey, cancellationToken))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.Tenant.KeyAlreadyExists);
        }

        var initialAdministratorUserId = request.InitialAdministratorUserId ?? request.ActorUserId;
        var initialAdministrator = await _users.GetForUpdateAsync(initialAdministratorUserId, cancellationToken)
            ?? throw new DomainNotFoundException(DomainErrorMessages.User.UserNotFound);

        if (!initialAdministrator.IsActive)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.User.UserInactive);
        }

        var tenant = new Domain.Entities.Tenant(request.TenantName, tenantKey);
        await _tenants.AddAsync(tenant, cancellationToken);

        Domain.Entities.TenantRole? ownerRole = null;
        foreach (var role in TenantRoleDefaults.All)
        {
            var createdRole = new Domain.Entities.TenantRole(
                tenant.Id,
                role.Key,
                role.Name,
                isSystem: true);
            await _roles.AddAsync(createdRole, cancellationToken);

            if (role.Key.Equals(TenantRoleDefaults.Owner, StringComparison.OrdinalIgnoreCase))
            {
                ownerRole = createdRole;
            }
        }

        if (ownerRole is null)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.AtLeastOneRoleRequired);
        }

        var membership = new TenantMembership(tenant.Id, initialAdministratorUserId, [ownerRole]);
        await _memberships.AddAsync(membership, cancellationToken);

        var applicationTenant = new ApplicationTenant(
            application.Id,
            tenant.Id,
            request.ExternalCustomerId,
            request.PlanCode);

        if (await _applicationTenants.MappingAlreadyExistsAsync(application.Id, tenant.Id, cancellationToken))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.ApplicationTenant.MappingAlreadyExists);
        }

        await _applicationTenants.AddAsync(applicationTenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _tenantResolutionCache.InvalidateByIdentifierAsync(tenantKey.Value, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.InitialAdministratorEmail)
            && request.InitialAdministratorUserId is null)
        {
            await _tenantService.InviteMemberAsync(
                new InviteMemberRequest
                {
                    TenantId = tenant.Id,
                    Email = request.InitialAdministratorEmail.Trim(),
                    Roles = [TenantRoleDefaults.Owner],
                    InvitedByUserId = request.ActorUserId,
                    ActorUserId = request.ActorUserId,
                    ActorPlatformRoles = request.ActorPlatformRoles
                },
                cancellationToken);
        }

        return new ProvisionApplicationTenantResult
        {
            ApplicationId = application.Id,
            TenantId = tenant.Id,
            MembershipId = membership.Id
        };
    }

    public async Task<ApplicationDto?> GetApplicationByIdAsync(
        GetApplicationByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var application = await _context.Applications
            .AsNoTracking()
            .Where(x => x.Id == request.ApplicationId)
            .Select(MapToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

        if (application is null)
        {
            return null;
        }

        var clients = await _clients.ListByApplicationIdAsync(request.ApplicationId, cancellationToken);
        return application with
        {
            Clients = clients.Select(MapClientSummary).ToList()
        };
    }

    public async Task<PagedResult<ApplicationDto>> ListApplicationsAsync(
        ListApplicationsRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var query = _context.Applications.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDtoExpression)
            .ToListAsync(cancellationToken);

        return new PagedResult<ApplicationDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ApplicationBrandingDto?> GetBrandingAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await _applications.GetByIdAsync(applicationId, cancellationToken);
        return application is null ? null : MapBrandingDto(application);
    }

    public async Task UpdateBrandingAsync(
        UpdateApplicationBrandingRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePlatformAdministrator(request.ActorPlatformRoles);

        var application = await _applications.GetForUpdateAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.Application.NotFound);

        if (application.IsSystem)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Application.SystemApplicationCannotBeModified);
        }

        if (request.BrandingEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.BrandingPrimaryColor) ||
                string.IsNullOrWhiteSpace(request.BrandingSecondaryColor))
            {
                throw new DomainValidationException(DomainErrorMessages.Application.BrandingColorsRequiredWhenEnabled);
            }
        }

        application.UpdateBranding(
            request.BrandingEnabled,
            request.BrandingPrimaryColor,
            request.BrandingSecondaryColor,
            request.BrandingHeroTitle,
            request.BrandingHeroSubtitle);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> UploadBrandingLogoAsync(
        UploadApplicationBrandingLogoRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsurePlatformAdministrator(request.ActorPlatformRoles);

        var application = await _applications.GetForUpdateAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.Application.NotFound);

        if (application.IsSystem)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Application.SystemApplicationCannotBeModified);
        }

        var logoPath = await _brandingStorage.SaveLogoAsync(
            application.Id,
            request.Content,
            request.ContentType,
            request.FileName,
            cancellationToken);

        application.SetBrandingLogoPath(logoPath);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return logoPath;
    }

    public async Task DeleteBrandingLogoAsync(
        Guid applicationId,
        IReadOnlyList<string> actorPlatformRoles,
        CancellationToken cancellationToken = default)
    {
        EnsurePlatformAdministrator(actorPlatformRoles);

        var application = await _applications.GetForUpdateAsync(applicationId, cancellationToken)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.Application.NotFound);

        if (application.IsSystem)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Application.SystemApplicationCannotBeModified);
        }

        await _brandingStorage.DeleteLogoAsync(applicationId, cancellationToken);
        application.ClearBrandingLogoPath();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void EnsurePlatformAdministrator(IReadOnlyList<string> actorPlatformRoles)
    {
        if (!actorPlatformRoles.Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role)))
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.UserHasNoTenantAccess);
        }
    }

    private static ApplicationBrandingDto MapBrandingDto(Domain.Entities.Application application) =>
        new()
        {
            ApplicationId = application.Id,
            BrandingEnabled = application.BrandingEnabled,
            BrandingPrimaryColor = application.BrandingPrimaryColor,
            BrandingSecondaryColor = application.BrandingSecondaryColor,
            BrandingLogoUrl = application.BrandingLogoPath,
            BrandingHeroTitle = application.BrandingHeroTitle,
            BrandingHeroSubtitle = application.BrandingHeroSubtitle
        };

    private static ApplicationClientSummaryDto MapClientSummary(ApplicationClient client) =>
        new()
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientType = client.ClientType,
            RedirectUris = client.RedirectUris,
            AllowedScopes = client.AllowedScopes,
            AccessTokenTtlSeconds = client.AccessTokenTtlSeconds,
            IsSystem = client.IsSystem
        };

    private static readonly System.Linq.Expressions.Expression<Func<Domain.Entities.Application, ApplicationDto>> MapToDtoExpression =
        x => new ApplicationDto
        {
            Id = x.Id,
            Name = x.Name,
            Slug = x.Slug,
            Type = x.Type,
            IsSystem = x.IsSystem,
            BrandingEnabled = x.BrandingEnabled,
            BrandingPrimaryColor = x.BrandingPrimaryColor,
            BrandingSecondaryColor = x.BrandingSecondaryColor,
            BrandingLogoUrl = x.BrandingLogoPath,
            BrandingHeroTitle = x.BrandingHeroTitle,
            BrandingHeroSubtitle = x.BrandingHeroSubtitle
        };
}
