namespace Kyvo.Domain.Models;

public class FederatedProviderConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? Issuer { get; set; } // Required only for GenericOidc
}
