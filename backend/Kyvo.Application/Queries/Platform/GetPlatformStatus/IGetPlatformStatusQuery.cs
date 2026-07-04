namespace Kyvo.Application.Queries.Platform.GetPlatformStatus;

public interface IGetPlatformStatusQuery
{
    Task<PlatformStatusResult> ExecuteAsync(CancellationToken ct = default);
}
