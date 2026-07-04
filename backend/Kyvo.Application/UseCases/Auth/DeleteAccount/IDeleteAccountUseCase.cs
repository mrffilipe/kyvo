namespace Kyvo.Application.UseCases.Auth.DeleteAccount;

public interface IDeleteAccountUseCase
{
    Task ExecuteAsync(CancellationToken ct = default);
}
