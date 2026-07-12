namespace Kyvo.Application.UseCases.IdentityProviders.EnableIdentityProvider;

public interface IEnableIdentityProviderUseCase
{
    Task ExecuteAsync(Guid id, CancellationToken ct = default);
}
