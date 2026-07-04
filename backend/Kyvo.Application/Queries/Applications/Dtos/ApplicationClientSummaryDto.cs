using Kyvo.Domain.Enums;

namespace Kyvo.Application.Queries.Applications.Dtos;

public sealed record ApplicationClientSummaryDto
{
    public required Guid Id { get; init; }
    public required string ClientId { get; init; }
    public required ClientType ClientType { get; init; }
    public required IReadOnlyList<string> RedirectUris { get; init; }
    public required IReadOnlyList<string> PostLogoutRedirectUris { get; init; }
    public required IReadOnlyList<string> AllowedScopes { get; init; }
    public required int AccessTokenTtlSeconds { get; init; }
    public required bool IsSystem { get; init; }
}
