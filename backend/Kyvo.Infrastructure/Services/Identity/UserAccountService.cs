using Kyvo.Application.Ports.Identity;
using Kyvo.Domain.Entities;
using Kyvo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Kyvo.Infrastructure.Services.Identity;

public sealed class UserAccountService : IUserAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAccountService(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var entity = await _userManager.FindByEmailAsync(email.Trim());
        return entity is null ? null : UserMapper.ToDomain(entity);
    }

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _userManager.FindByIdAsync(id.ToString("D"));
        return entity is null ? null : UserMapper.ToDomain(entity);
    }

    public async Task<User?> FindByLoginAsync(string providerAlias, string providerUserId, CancellationToken ct = default)
    {
        var entity = await _userManager.FindByLoginAsync(providerAlias, providerUserId);
        return entity is null ? null : UserMapper.ToDomain(entity);
    }

    public async Task<UserAccountOperationResult> CreateWithPasswordAsync(User user, string password, CancellationToken ct = default)
    {
        var entity = UserMapper.ToNewPersistence(user);
        entity.SetCreatedAt();
        var result = await _userManager.CreateAsync(entity, password);
        return MapResult(result);
    }

    public async Task<UserAccountOperationResult> CreateAsync(User user, CancellationToken ct = default)
    {
        var entity = UserMapper.ToNewPersistence(user);
        entity.SetCreatedAt();
        var result = await _userManager.CreateAsync(entity);
        return MapResult(result);
    }

    public async Task<UserAccountOperationResult> AddLoginAsync(
        Guid userId,
        string providerAlias,
        string providerUserId,
        CancellationToken ct = default)
    {
        var entity = await _userManager.FindByIdAsync(userId.ToString("D"));
        if (entity is null)
        {
            return UserAccountOperationResult.Failure(["User not found."]);
        }

        var result = await _userManager.AddLoginAsync(
            entity,
            new UserLoginInfo(providerAlias, providerUserId, providerAlias));

        return MapResult(result);
    }

    public async Task<bool> HasPasswordAsync(Guid userId, CancellationToken ct = default)
    {
        var entity = await _userManager.FindByIdAsync(userId.ToString("D"));
        return entity is not null && await _userManager.HasPasswordAsync(entity);
    }

    public async Task<UserAccountOperationResult> AddPasswordAsync(Guid userId, string password, CancellationToken ct = default)
    {
        var entity = await _userManager.FindByIdAsync(userId.ToString("D"));
        if (entity is null)
        {
            return UserAccountOperationResult.Failure(["User not found."]);
        }

        var result = await _userManager.AddPasswordAsync(entity, password);
        return MapResult(result);
    }

    private static UserAccountOperationResult MapResult(IdentityResult result) =>
        result.Succeeded
            ? UserAccountOperationResult.Success()
            : UserAccountOperationResult.Failure(result.Errors.Select(e => e.Description));
}
