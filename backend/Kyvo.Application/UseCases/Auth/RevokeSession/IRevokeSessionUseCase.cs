namespace Kyvo.Application.UseCases.Auth.RevokeSession;

public interface IRevokeSessionUseCase
{
    Task ExecuteAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
}
