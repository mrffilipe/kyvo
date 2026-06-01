namespace Kyvo.Application.Services.IdentityProvider;

public interface IAccountLoginPageStateProvider
{
    Task<AccountLoginPageState> GetStateAsync(CancellationToken cancellationToken = default);
}
