using Kyvo.API.Common;
using Kyvo.Application.Queries.Platform.GetPlatformStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Controllers;

/// <summary>
/// Platform lifecycle and configuration status.
/// </summary>
public sealed class PlatformController : V1ApiControllerBase
{
    private readonly IGetPlatformStatusQuery _getPlatformStatusQuery;

    public PlatformController(IGetPlatformStatusQuery getPlatformStatusQuery) =>
        _getPlatformStatusQuery = getPlatformStatusQuery;

    /// <summary>
    /// Returns whether the platform has been bootstrapped and which OAuth client to use for admin login.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PlatformStatusResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformStatusResult>> GetStatus(CancellationToken ct)
    {
        var result = await _getPlatformStatusQuery.ExecuteAsync(ct);
        return Ok(result);
    }
}
