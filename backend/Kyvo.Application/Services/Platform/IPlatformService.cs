namespace Kyvo.Application.Services.Platform;

public interface IPlatformService
{
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);

    Task<PlatformStatusResult> GetStatusAsync(CancellationToken cancellationToken = default);
}
