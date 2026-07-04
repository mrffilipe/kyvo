namespace Kyvo.Application.UseCases.IdentityProviders.UpdateIdentityProvider;

public interface IUpdateIdentityProviderUseCase
{
    Task ExecuteAsync(UpdateIdentityProviderRequest request, CancellationToken ct = default);
}
