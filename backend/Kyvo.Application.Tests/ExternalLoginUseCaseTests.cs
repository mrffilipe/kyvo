using Kyvo.Application.Ports.Identity;
using Kyvo.Application.UseCases.Auth;
using Kyvo.Application.UseCases.Auth.ExternalLogin;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Kyvo.Application.Tests;

public sealed class ExternalLoginUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesUserWhenNotFound()
    {
        var userAccounts = new Mock<IUserAccountService>();
        User? createdUser = null;

        userAccounts.Setup(x => x.FindByLoginAsync("google", "sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        userAccounts.Setup(x => x.FindByEmailAsync("new@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        userAccounts.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => createdUser = u)
            .ReturnsAsync(UserAccountOperationResult.Success());
        userAccounts.Setup(x => x.AddLoginAsync(It.IsAny<Guid>(), "google", "sub-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserAccountOperationResult.Success());

        var memberships = new Mock<ITenantMembershipRepository>();
        memberships.Setup(x => x.ListByUserIdWithTenantAndRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var platformRoles = new Mock<IUserPlatformRoleRepository>();
        platformRoles.Setup(x => x.ListByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var useCase = new ExternalLoginUseCase(userAccounts.Object, memberships.Object, platformRoles.Object);

        var result = await useCase.ExecuteAsync(new ExternalLoginRequest
        {
            ProviderAlias = "google",
            ProviderUserId = "sub-1",
            Email = "new@test.com",
            DisplayName = "New User"
        });

        Assert.NotNull(createdUser);
        Assert.Equal("new@test.com", createdUser!.Email);
        Assert.Equal(result.UserId, createdUser.Id);
        userAccounts.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
