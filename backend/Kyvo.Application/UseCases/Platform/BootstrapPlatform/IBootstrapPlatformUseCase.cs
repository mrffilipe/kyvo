namespace Kyvo.Application.UseCases.Platform.BootstrapPlatform;

public interface IBootstrapPlatformUseCase
{
    Task ExecuteAsync(CancellationToken ct = default);
}
