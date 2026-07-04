using Kyvo.Application.UseCases.Auth;

namespace Kyvo.API.Models;

/// <summary>
/// Response for SaaS tenant subscription. Clients that need fresh <c>tid</c>/<c>trole</c> claims after
/// subscribing use the existing OAuth refresh token against <c>/connect/token</c> (same pattern already
/// used after <c>switch-tenant</c>), so no tokens are minted out-of-band here.
/// </summary>
public sealed record SubscribeTenantResponse
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? MembershipId { get; init; }
    public IReadOnlyList<string> TenantRoles { get; init; } = [];
    public IReadOnlyList<string> PlatformRoles { get; init; } = [];
    public IReadOnlyList<AuthTenantSummaryDto> Tenants { get; init; } = [];
}
