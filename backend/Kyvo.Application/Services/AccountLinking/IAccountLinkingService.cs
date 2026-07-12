using Kyvo.Application.Services.Claims;

namespace Kyvo.Application.Services.AccountLinking;

public interface IAccountLinkingService
{
    Task<AccountLinkResult> LinkOrCreateAsync(MappedExternalIdentity identity, CancellationToken ct = default);
}

public sealed class AccountLinkResult
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required bool Created { get; init; }
    public required bool Linked { get; init; }
}
