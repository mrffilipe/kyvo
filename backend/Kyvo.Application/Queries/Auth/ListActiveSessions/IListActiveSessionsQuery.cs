using Kyvo.Application.Queries.Auth.Dtos;

namespace Kyvo.Application.Queries.Auth.ListActiveSessions;

public interface IListActiveSessionsQuery
{
    Task<IReadOnlyList<AuthSessionDto>> ExecuteAsync(Guid userId, CancellationToken ct = default);
}
