using Kyvo.Application.Ports.Federation;

namespace Kyvo.Infrastructure.Services.Federation;

public sealed class FederatedProviderRegistrationCache : IFederatedProviderRegistrationCache
{
    private readonly DynamicOpenIddictClientService _clientService;
    private readonly OpenIddictClientEndpointConfigurer _endpointConfigurer;

    public FederatedProviderRegistrationCache(DynamicOpenIddictClientService clientService, OpenIddictClientEndpointConfigurer endpointConfigurer)
    {
        _clientService = clientService;
        _endpointConfigurer = endpointConfigurer;
    }

    public void Invalidate(string alias)
    {
        _clientService.Invalidate(alias);
        _endpointConfigurer.Reload();
    }
}
