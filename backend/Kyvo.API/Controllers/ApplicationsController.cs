using Kyvo.API.Common;
using Kyvo.API.Models;
using Kyvo.Application.Common;
using Kyvo.Application.Queries.Applications.CheckApplicationSlugAvailability;
using Kyvo.Application.Queries.Applications.GetApplicationBranding;
using Kyvo.Application.Queries.Applications.GetApplicationById;
using Kyvo.Application.Queries.Applications.ListApplications;
using Kyvo.Application.Services.UserScope;
using Kyvo.Application.UseCases.Applications.CreateApplication;
using Kyvo.Application.UseCases.Applications.CreateApplicationClient;
using Kyvo.Application.UseCases.Applications.DeleteApplicationBrandingLogo;
using Kyvo.Application.UseCases.Applications.ProvisionTenant;
using Kyvo.Application.UseCases.Applications.UpdateApplicationBranding;
using Kyvo.Application.UseCases.Applications.UploadApplicationBrandingLogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kyvo.Application.Queries.Applications.Dtos;

namespace Kyvo.API.Controllers;

/// <summary>
/// Manages SaaS applications, OAuth clients, and tenant provisioning for an application.
/// </summary>
public sealed class ApplicationsController : V1ApiControllerBase
{
    private readonly IUserScope _userScope;
    private readonly ICreateApplicationUseCase _createApplicationUseCase;
    private readonly ICreateApplicationClientUseCase _createApplicationClientUseCase;
    private readonly IProvisionTenantUseCase _provisionTenantUseCase;
    private readonly IListApplicationsQuery _listApplicationsQuery;
    private readonly ICheckApplicationSlugAvailabilityQuery _checkApplicationSlugAvailabilityQuery;
    private readonly IGetApplicationByIdQuery _getApplicationByIdQuery;
    private readonly IGetApplicationBrandingQuery _getApplicationBrandingQuery;
    private readonly IUpdateApplicationBrandingUseCase _updateApplicationBrandingUseCase;
    private readonly IUploadApplicationBrandingLogoUseCase _uploadApplicationBrandingLogoUseCase;
    private readonly IDeleteApplicationBrandingLogoUseCase _deleteApplicationBrandingLogoUseCase;

    public ApplicationsController(
        IUserScope userScope,
        ICreateApplicationUseCase createApplicationUseCase,
        ICreateApplicationClientUseCase createApplicationClientUseCase,
        IProvisionTenantUseCase provisionTenantUseCase,
        IListApplicationsQuery listApplicationsQuery,
        ICheckApplicationSlugAvailabilityQuery checkApplicationSlugAvailabilityQuery,
        IGetApplicationByIdQuery getApplicationByIdQuery,
        IGetApplicationBrandingQuery getApplicationBrandingQuery,
        IUpdateApplicationBrandingUseCase updateApplicationBrandingUseCase,
        IUploadApplicationBrandingLogoUseCase uploadApplicationBrandingLogoUseCase,
        IDeleteApplicationBrandingLogoUseCase deleteApplicationBrandingLogoUseCase)
    {
        _userScope = userScope;
        _createApplicationUseCase = createApplicationUseCase;
        _createApplicationClientUseCase = createApplicationClientUseCase;
        _provisionTenantUseCase = provisionTenantUseCase;
        _listApplicationsQuery = listApplicationsQuery;
        _checkApplicationSlugAvailabilityQuery = checkApplicationSlugAvailabilityQuery;
        _getApplicationByIdQuery = getApplicationByIdQuery;
        _getApplicationBrandingQuery = getApplicationBrandingQuery;
        _updateApplicationBrandingUseCase = updateApplicationBrandingUseCase;
        _uploadApplicationBrandingLogoUseCase = uploadApplicationBrandingLogoUseCase;
        _deleteApplicationBrandingLogoUseCase = deleteApplicationBrandingLogoUseCase;
    }

    [Authorize(Policy = "PlatformAdministrator")]
    [HttpPost]
    [ProducesResponseType(typeof(CreatedIdResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreatedIdResponse>> CreateApplication([FromBody] CreateApplicationRequest request, CancellationToken ct)
    {
        var id = await _createApplicationUseCase.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(GetApplicationById), new { id, version = "1.0" }, new CreatedIdResponse { Id = id });
    }

    [HttpPost("{applicationId:guid}/clients")]
    [ProducesResponseType(typeof(CreatedIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatedIdResponse>> CreateApplicationClient(Guid applicationId, [FromBody] CreateApplicationClientRequest request, CancellationToken ct)
    {
        var id = await _createApplicationClientUseCase.ExecuteAsync(
            request with
            {
                ApplicationId = applicationId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return Ok(new CreatedIdResponse { Id = id });
    }

    [Authorize(Policy = "PlatformAdministrator")]
    [HttpPost("{applicationId:guid}/tenants/provision")]
    [ProducesResponseType(typeof(ProvisionApplicationTenantResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProvisionApplicationTenantResult>> ProvisionTenant(Guid applicationId, [FromBody] ProvisionApplicationTenantRequest request, CancellationToken ct)
    {
        var result = await _provisionTenantUseCase.ExecuteAsync(
            request with
            {
                ApplicationId = applicationId,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ApplicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ApplicationDto>>> ListApplications([FromQuery] ListApplicationsRequest request, CancellationToken ct)
    {
        var result = await _listApplicationsQuery.ExecuteAsync(request, ct);
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdministrator")]
    [HttpGet("slugs/{slug}/availability")]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AvailabilityDto>> CheckSlugAvailability(string slug, CancellationToken ct)
    {
        var result = await _checkApplicationSlugAvailabilityQuery.ExecuteAsync(slug, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationDto>> GetApplicationById(Guid id, CancellationToken ct)
    {
        var result = await _getApplicationByIdQuery.ExecuteAsync(
            new GetApplicationByIdRequest { ApplicationId = id },
            ct);

        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "PlatformAdministrator")]
    [HttpGet("{id:guid}/branding")]
    [ProducesResponseType(typeof(ApplicationBrandingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationBrandingDto>> GetApplicationBranding(Guid id, CancellationToken ct)
    {
        var result = await _getApplicationBrandingQuery.ExecuteAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "PlatformAdministrator")]
    [HttpPatch("{id:guid}/branding")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApplicationBranding(Guid id, [FromBody] UpdateApplicationBrandingBody body, CancellationToken ct)
    {
        await _updateApplicationBrandingUseCase.ExecuteAsync(
            new UpdateApplicationBrandingRequest
            {
                ApplicationId = id,
                BrandingEnabled = body.BrandingEnabled,
                BrandingPrimaryColor = body.BrandingPrimaryColor,
                BrandingSecondaryColor = body.BrandingSecondaryColor,
                BrandingHeroTitle = body.BrandingHeroTitle,
                BrandingHeroSubtitle = body.BrandingHeroSubtitle,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return NoContent();
    }

    [Authorize(Policy = "PlatformAdministrator")]
    [HttpPost("{id:guid}/branding/logo")]
    [RequestSizeLimit(512 * 1024)]
    [ProducesResponseType(typeof(ApplicationBrandingLogoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationBrandingLogoResponse>> UploadApplicationBrandingLogo(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            return BadRequest();
        }

        await using var stream = file.OpenReadStream();
        var logoUrl = await _uploadApplicationBrandingLogoUseCase.ExecuteAsync(
            new UploadApplicationBrandingLogoRequest
            {
                ApplicationId = id,
                Content = stream,
                ContentType = file.ContentType,
                FileName = file.FileName,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            ct);

        return Ok(new ApplicationBrandingLogoResponse { BrandingLogoUrl = logoUrl });
    }

    [Authorize(Policy = "PlatformAdministrator")]
    [HttpDelete("{id:guid}/branding/logo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteApplicationBrandingLogo(Guid id, CancellationToken ct)
    {
        await _deleteApplicationBrandingLogoUseCase.ExecuteAsync(id, _userScope.PlatformRoles, ct);
        return NoContent();
    }
}
