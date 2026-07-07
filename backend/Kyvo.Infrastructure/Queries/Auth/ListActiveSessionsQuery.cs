using Kyvo.Application.Queries.Auth.ListActiveSessions;
using Kyvo.Domain.Repositories;
using Kyvo.Application.Queries.Auth.Dtos;

namespace Kyvo.Infrastructure.Queries.Auth;

public sealed class ListActiveSessionsQuery : IListActiveSessionsQuery
{
    private readonly IAuthSessionRepository _sessions;

    public ListActiveSessionsQuery(IAuthSessionRepository sessions) => _sessions = sessions;

    public async Task<IReadOnlyList<AuthSessionDto>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var sessions = await _sessions.ListActiveByUserIdAsync(userId, ct);
        return sessions
            .Select(session => new AuthSessionDto
            {
                SessionId = session.Id,
                TenantId = session.TenantId,
                MembershipId = session.MembershipId,
                Status = session.Status,
                UserAgent = session.UserAgent,
                IpAddress = session.IpAddress,
                ExpiresAt = session.ExpiresAt,
                LastActivityAt = session.LastActivityAt
            })
            .ToList();
    }
}
