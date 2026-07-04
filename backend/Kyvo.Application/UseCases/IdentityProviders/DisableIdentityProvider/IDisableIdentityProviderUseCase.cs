namespace Kyvo.Application.UseCases.IdentityProviders.DisableIdentityProvider;

public interface IDisableIdentityProviderUseCase
{
    Task ExecuteAsync(Guid id, CancellationToken ct = default);
}
