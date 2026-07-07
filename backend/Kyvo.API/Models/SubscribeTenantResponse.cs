using Kyvo.Application.UseCases.Auth;

namespace Kyvo.API.Models;

/// <summary>
/// Response for SaaS tenant subscription including a tenant-scoped access token.
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
    public string? AccessToken { get; init; }
    public int? ExpiresIn { get; init; }
    public string? TokenType { get; init; }
}
