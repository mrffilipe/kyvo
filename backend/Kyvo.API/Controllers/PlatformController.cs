using Kyvo.API.Common;
using Kyvo.Application.Services.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Controllers;

/// <summary>
/// Platform lifecycle and configuration status.
/// </summary>
public sealed class PlatformController : V1ApiControllerBase
{
    private readonly IPlatformService _platformService;

    public PlatformController(IPlatformService platformService) => _platformService = platformService;

    /// <summary>
    /// Returns whether the platform has been bootstrapped and which OAuth client to use for admin login.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PlatformStatusResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformStatusResult>> GetStatus(CancellationToken cancellationToken)
    {
        var result = await _platformService.GetStatusAsync(cancellationToken);
        return Ok(result);
    }
}
