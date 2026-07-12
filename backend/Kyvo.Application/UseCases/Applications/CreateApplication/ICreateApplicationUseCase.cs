namespace Kyvo.Application.UseCases.Applications.CreateApplication;

public interface ICreateApplicationUseCase
{
    Task<Guid> ExecuteAsync(CreateApplicationRequest request, CancellationToken ct = default);
}
