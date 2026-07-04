namespace Kyvo.Application.UseCases.Applications.CreateApplicationClient;

public interface ICreateApplicationClientUseCase
{
    Task<Guid> ExecuteAsync(CreateApplicationClientRequest request, CancellationToken ct = default);
}
