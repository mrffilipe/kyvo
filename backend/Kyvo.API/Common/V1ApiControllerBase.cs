using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace Kyvo.API.Common;

/// <summary>
/// Base for versioned JSON API controllers under <c>/v{version}/...</c>, protected by OpenIddict's
/// validation handler (validates access tokens issued by our own OpenIddict Server).
/// </summary>
[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
[Produces("application/json")]
public abstract class V1ApiControllerBase : ControllerBase
{
}
