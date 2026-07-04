using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.UseCases.Auth.RevokeSession;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Auth;

public sealed class RevokeSessionUseCase : IRevokeSessionUseCase
{
    private readonly IAuthSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeSessionUseCase(IAuthSessionRepository sessions, IUnitOfWork unitOfWork)
    {
        _sessions = sessions;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var session = await _sessions.GetForUpdateAsync(sessionId, ct)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.Auth.SESSION_NOT_FOUND);

        if (session.UserId != userId)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.CANNOT_REVOKE_ANOTHER_USER_SESSION);
        }

        session.Revoke();
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
