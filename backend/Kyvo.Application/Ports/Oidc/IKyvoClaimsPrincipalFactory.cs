using System.Security.Claims;
using Kyvo.Domain.Entities;

namespace Kyvo.Application.Ports.Oidc;

/// <summary>
/// Builds the claims principal signed into OpenIddict access/id tokens: subject, tenant context
/// (<c>tid</c>/<c>mid</c>/<c>trole</c>), platform roles (<c>prole</c>) and the technical claims
/// (<c>uid</c>/<c>sid</c>) consumed by <c>IUserScope</c>/TenancyKit.
/// </summary>
public interface IKyvoClaimsPrincipalFactory
{
    Task<ClaimsPrincipal> CreateAsync(
        User user,
        AuthSession session,
        string clientId,
        IReadOnlyList<string> scopes,
        CancellationToken ct = default);
}
