namespace Kyvo.Application.UseCases.Auth.ExternalLogin;

public interface IExternalLoginUseCase
{
    Task<ExternalLoginResult> ExecuteAsync(ExternalLoginRequest request, CancellationToken ct = default);
    Task<ExternalLoginResult> BuildResultForUserAsync(Kyvo.Domain.Entities.User user, CancellationToken ct = default);
}
