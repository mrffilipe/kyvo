namespace Kyvo.Application.UseCases.Auth;

public sealed record TenantContextResult
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? MembershipId { get; init; }
    public required IReadOnlyList<string> TenantRoles { get; init; }
    public required IReadOnlyList<string> PlatformRoles { get; init; }
    public required IReadOnlyList<AuthTenantSummaryDto> Tenants { get; init; }
    public string? AccessToken { get; init; }
    public int? ExpiresIn { get; init; }
    public string? TokenType { get; init; }
}
