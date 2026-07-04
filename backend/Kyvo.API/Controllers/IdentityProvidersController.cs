using Kyvo.API.Common;
using Kyvo.Application.Common;
using Kyvo.Application.Queries.IdentityProviders.CheckIdentityProviderAliasAvailability;
using Kyvo.Application.Queries.IdentityProviders.GetIdentityProviderById;
using Kyvo.Application.Queries.IdentityProviders.ListIdentityProviders;
using Kyvo.Application.UseCases.IdentityProviders.AddIdentityProvider;
using Kyvo.Application.UseCases.IdentityProviders.UpdateIdentityProvider;
using Kyvo.Application.UseCases.IdentityProviders.DisableIdentityProvider;
using Kyvo.Application.UseCases.IdentityProviders.EnableIdentityProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kyvo.Application.Queries.IdentityProviders.Dtos;

namespace Kyvo.API.Controllers;

/// <summary>
/// Platform-wide identity provider configuration (Firebase, local password, etc.).
/// </summary>
[Authorize(Policy = "PlatformAdministrator")]
public sealed class IdentityProvidersController : V1ApiControllerBase
{
    private readonly IAddIdentityProviderUseCase _addIdentityProviderUseCase;
    private readonly IListIdentityProvidersQuery _listIdentityProvidersQuery;
    private readonly ICheckIdentityProviderAliasAvailabilityQuery _checkIdentityProviderAliasAvailabilityQuery;
    private readonly IGetIdentityProviderByIdQuery _getIdentityProviderByIdQuery;
    private readonly IUpdateIdentityProviderUseCase _updateIdentityProviderUseCase;
    private readonly IEnableIdentityProviderUseCase _enableIdentityProviderUseCase;
    private readonly IDisableIdentityProviderUseCase _disableIdentityProviderUseCase;

    public IdentityProvidersController(
        IAddIdentityProviderUseCase addIdentityProviderUseCase,
        IListIdentityProvidersQuery listIdentityProvidersQuery,
        ICheckIdentityProviderAliasAvailabilityQuery checkIdentityProviderAliasAvailabilityQuery,
        IGetIdentityProviderByIdQuery getIdentityProviderByIdQuery,
        IUpdateIdentityProviderUseCase updateIdentityProviderUseCase,
        IEnableIdentityProviderUseCase enableIdentityProviderUseCase,
        IDisableIdentityProviderUseCase disableIdentityProviderUseCase)
    {
        _addIdentityProviderUseCase = addIdentityProviderUseCase;
        _listIdentityProvidersQuery = listIdentityProvidersQuery;
        _checkIdentityProviderAliasAvailabilityQuery = checkIdentityProviderAliasAvailabilityQuery;
        _getIdentityProviderByIdQuery = getIdentityProviderByIdQuery;
        _updateIdentityProviderUseCase = updateIdentityProviderUseCase;
        _enableIdentityProviderUseCase = enableIdentityProviderUseCase;
        _disableIdentityProviderUseCase = disableIdentityProviderUseCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddIdentityProviderResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AddIdentityProviderResult>> Add([FromBody] AddIdentityProviderRequest request, CancellationToken ct)
    {
        var result = await _addIdentityProviderUseCase.ExecuteAsync(request, ct);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IdentityProviderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IdentityProviderDto>>> List(CancellationToken ct)
    {
        var result = await _listIdentityProvidersQuery.ExecuteAsync(ct);
        return Ok(result);
    }

    [HttpGet("aliases/{alias}/availability")]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AvailabilityDto>> CheckAliasAvailability(string alias, CancellationToken ct)
    {
        var result = await _checkIdentityProviderAliasAvailabilityQuery.ExecuteAsync(alias, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IdentityProviderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IdentityProviderDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _getIdentityProviderByIdQuery.ExecuteAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIdentityProviderRequest request, CancellationToken ct)
    {
        await _updateIdentityProviderUseCase.ExecuteAsync(
            request with { Id = id },
            ct);

        return NoContent();
    }

    [HttpPost("{id:guid}/enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enable(Guid id, CancellationToken ct)
    {
        await _enableIdentityProviderUseCase.ExecuteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disable(Guid id, CancellationToken ct)
    {
        await _disableIdentityProviderUseCase.ExecuteAsync(id, ct);
        return NoContent();
    }
}
