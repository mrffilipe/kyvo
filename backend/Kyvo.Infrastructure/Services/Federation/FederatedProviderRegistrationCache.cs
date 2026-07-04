using Kyvo.Application.Ports.Federation;

namespace Kyvo.Infrastructure.Services.Federation;

public sealed class FederatedProviderRegistrationCache : IFederatedProviderRegistrationCache
{
    private readonly DynamicOpenIddictClientService _clientService;

    public FederatedProviderRegistrationCache(DynamicOpenIddictClientService clientService) => _clientService = clientService;

    public void Invalidate(string alias) => _clientService.Invalidate(alias);
}
