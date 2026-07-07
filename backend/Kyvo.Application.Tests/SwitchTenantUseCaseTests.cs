using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.Services.UserScope;
using Kyvo.Application.UseCases.Auth;
using Kyvo.Application.UseCases.Auth.SwitchTenant;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Kyvo.Application.Tests;

public sealed class SwitchTenantUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsTenantAccessToken()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var user = User.Restore(userId, "user@test.com", "User", null, true, DateTime.UtcNow, DateTime.UtcNow);
        var session = new AuthSession(userId, null, null, DateTime.UtcNow.AddHours(1), "agent", "127.0.0.1");
        var role = new TenantRole(tenantId, new TenantRoleKey("owner"), "Owner", isSystem: true);
        var membership = new TenantMembership(tenantId, userId, [role]);
        var tenant = new Tenant("Acme", new TenantKey("acme"));
        typeof(TenantMembership).GetProperty(nameof(TenantMembership.Tenant))!
            .SetValue(membership, tenant);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetForUpdateAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var memberships = new Mock<ITenantMembershipRepository>();
        memberships.Setup(x => x.GetByUserIdAndTenantIdWithRolesAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        memberships.Setup(x => x.ListByUserIdWithTenantAndRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([membership]);

        var sessions = new Mock<IAuthSessionRepository>();
        sessions.Setup(x => x.GetForUpdateAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var platformRoles = new Mock<IUserPlatformRoleRepository>();
        platformRoles.Setup(x => x.ListByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var userScope = new Mock<IUserScope>();
        userScope.SetupGet(x => x.IsAuthenticated).Returns(true);
        userScope.SetupGet(x => x.UserId).Returns(userId);
        userScope.SetupGet(x => x.SessionId).Returns(sessionId);

        var unitOfWork = new Mock<IUnitOfWork>();
        var tokenIssuer = new Mock<ITenantAccessTokenIssuer>();
        tokenIssuer.Setup(x => x.IssueToken(session, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns("tenant-jwt");

        var useCase = new SwitchTenantUseCase(
            users.Object,
            memberships.Object,
            sessions.Object,
            platformRoles.Object,
            userScope.Object,
            unitOfWork.Object,
            tokenIssuer.Object);

        var result = await useCase.ExecuteAsync(new SwitchTenantRequest { TenantId = tenantId });

        Assert.Equal("tenant-jwt", result.AccessToken);
        Assert.Equal(900, result.ExpiresIn);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(tenantId, result.TenantId);
    }
}
