using Kyvo.Application.UseCases.IdentityProviders.AddIdentityProvider;

namespace Kyvo.Application.UseCases.IdentityProviders.AddIdentityProvider;

public interface IAddIdentityProviderUseCase
{
    Task<AddIdentityProviderResult> ExecuteAsync(AddIdentityProviderRequest request, CancellationToken ct = default);
}
