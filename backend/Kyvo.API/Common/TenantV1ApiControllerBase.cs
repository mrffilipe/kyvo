using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kyvo.API.Common;

/// <summary>
/// Base for tenant-scoped API controllers requiring a tenant access token (<c>token_use=tenant</c>).
/// </summary>
[Authorize(Policy = "RequireTenantToken")]
public abstract class TenantV1ApiControllerBase : V1ApiControllerBase
{
}
