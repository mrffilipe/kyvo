namespace Kyvo.Application.Services.Oidc;

public interface IPlatformAdminConsoleAccessGuard
{
    Task<OidcError?> TryValidateAccessAsync(Guid userId, string clientId, CancellationToken ct = default);
}
