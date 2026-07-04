using Kyvo.Domain.Entities;

namespace Kyvo.Application.Ports.Identity;

public sealed record UserAccountOperationResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static UserAccountOperationResult Success() => new(true, []);

    public static UserAccountOperationResult Failure(IEnumerable<string> errors) =>
        new(false, errors.ToList());
}

public interface IUserAccountService
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> FindByLoginAsync(string providerAlias, string providerUserId, CancellationToken ct = default);
    Task<UserAccountOperationResult> CreateWithPasswordAsync(User user, string password, CancellationToken ct = default);
    Task<UserAccountOperationResult> CreateAsync(User user, CancellationToken ct = default);
    Task<UserAccountOperationResult> AddLoginAsync(
        Guid userId,
        string providerAlias,
        string providerUserId,
        CancellationToken ct = default);
    Task<bool> HasPasswordAsync(Guid userId, CancellationToken ct = default);
    Task<UserAccountOperationResult> AddPasswordAsync(Guid userId, string password, CancellationToken ct = default);
}
