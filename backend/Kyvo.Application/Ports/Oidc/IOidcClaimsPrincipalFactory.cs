using System.Security.Claims;
using Kyvo.Domain.Entities;

namespace Kyvo.Application.Ports.Oidc;

/// <summary>
/// Builds a pure OIDC claims principal (platform context only — no tenant claims).
/// </summary>
public interface IOidcClaimsPrincipalFactory
{
    Task<ClaimsPrincipal> CreateAsync(
        User user,
        AuthSession session,
        string clientId,
        IReadOnlyList<string> scopes,
        CancellationToken ct = default);
}
