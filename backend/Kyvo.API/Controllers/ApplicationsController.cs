using Kyvo.API.Common;
using Kyvo.API.Models;
using Kyvo.Application.Common;
using Kyvo.Application.Services.AppService;
using Kyvo.Application.Services.UserScope;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Controllers;

/// <summary>
/// Manages SaaS applications, OAuth clients, and tenant provisioning for an application.
/// </summary>
public sealed class ApplicationsController : V1ApiControllerBase
{
    private readonly IUserScope _userScope;
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IUserScope userScope, IApplicationService applicationService)
    {
        _userScope = userScope;
        _applicationService = applicationService;
    }

    /// <summary>
    /// Registers a new application (platform administrators only).
    /// </summary>
    [Authorize(Policy = "PlatformAdministrator")]
    [HttpPost]
    [ProducesResponseType(typeof(CreatedIdResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreatedIdResponse>> CreateApplication(
        [FromBody] CreateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _applicationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetApplicationById), new { id, version = "1.0" }, new CreatedIdResponse(id));
    }

    /// <summary>
    /// Creates an OAuth/OIDC client for the given application.
    /// </summary>
    [HttpPost("{applicationId:guid}/clients")]
    [ProducesResponseType(typeof(CreatedIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatedIdResponse>> CreateApplicationClient(
        Guid applicationId,
        [FromBody] CreateApplicationClientRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _applicationService.CreateClientAsync(
            request with
            {
                ApplicationId = applicationId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            cancellationToken);

        return Ok(new CreatedIdResponse(id));
    }

    /// <summary>
    /// Provisions a tenant linked to an application (platform administrators only).
    /// </summary>
    [Authorize(Policy = "PlatformAdministrator")]
    [HttpPost("{applicationId:guid}/tenants/provision")]
    [ProducesResponseType(typeof(ProvisionApplicationTenantResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProvisionApplicationTenantResult>> ProvisionTenant(
        Guid applicationId,
        [FromBody] ProvisionApplicationTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.ProvisionTenantAsync(
            request with
            {
                ApplicationId = applicationId,
                ActorUserId = _userScope.UserId,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Lists applications with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ApplicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ApplicationDto>>> ListApplications(
        [FromQuery] ListApplicationsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.ListAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Checks whether an application slug is available.
    /// </summary>
    [Authorize(Policy = "PlatformAdministrator")]
    [HttpGet("slugs/{slug}/availability")]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AvailabilityDto>> CheckSlugAvailability(
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.IsSlugAvailableAsync(slug, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single application by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationDto>> GetApplicationById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetByIdAsync(
            new GetApplicationByIdRequest { ApplicationId = id },
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Returns login branding settings for an application.
    /// </summary>
    [Authorize(Policy = "PlatformAdministrator")]
    [HttpGet("{id:guid}/branding")]
    [ProducesResponseType(typeof(ApplicationBrandingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationBrandingDto>> GetApplicationBranding(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetBrandingAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Updates login branding (colors and enable flag) for an application.
    /// </summary>
    [Authorize(Policy = "PlatformAdministrator")]
    [HttpPatch("{id:guid}/branding")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApplicationBranding(
        Guid id,
        [FromBody] UpdateApplicationBrandingBody body,
        CancellationToken cancellationToken)
    {
        await _applicationService.UpdateBrandingAsync(
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
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Uploads a logo for the application login screen.
    /// </summary>
    [Authorize(Policy = "PlatformAdministrator")]
    [HttpPost("{id:guid}/branding/logo")]
    [RequestSizeLimit(512 * 1024)]
    [ProducesResponseType(typeof(ApplicationBrandingLogoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationBrandingLogoResponse>> UploadApplicationBrandingLogo(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest();
        }

        await using var stream = file.OpenReadStream();
        var logoUrl = await _applicationService.UploadBrandingLogoAsync(
            new UploadApplicationBrandingLogoRequest
            {
                ApplicationId = id,
                Content = stream,
                ContentType = file.ContentType,
                FileName = file.FileName,
                ActorPlatformRoles = _userScope.PlatformRoles
            },
            cancellationToken);

        return Ok(new ApplicationBrandingLogoResponse(logoUrl));
    }

    /// <summary>
    /// Removes the custom login logo for an application.
    /// </summary>
    [Authorize(Policy = "PlatformAdministrator")]
    [HttpDelete("{id:guid}/branding/logo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteApplicationBrandingLogo(Guid id, CancellationToken cancellationToken)
    {
        await _applicationService.DeleteBrandingLogoAsync(id, _userScope.PlatformRoles, cancellationToken);
        return NoContent();
    }
}
