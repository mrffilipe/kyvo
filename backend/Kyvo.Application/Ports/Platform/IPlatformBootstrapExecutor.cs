namespace Kyvo.Application.Ports.Platform;

/// <summary>
/// Runs platform bootstrap work inside a serializable database transaction so concurrent replicas
/// cannot double-initialize the platform.
/// </summary>
public interface IPlatformBootstrapExecutor
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);
}
