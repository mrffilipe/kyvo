using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace Kyvo.API.Common;

/// <summary>
/// Base for JSON API controllers under <c>/api/v1/...</c>, protected by OpenIddict's validation handler.
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[ApiVersion("1.0")]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public abstract class V1ApiControllerBase : ControllerBase
{
}
