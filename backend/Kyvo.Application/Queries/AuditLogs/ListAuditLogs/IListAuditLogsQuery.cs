using Kyvo.Application.Common;
using Kyvo.Application.Queries.AuditLogs.Dtos;

namespace Kyvo.Application.Queries.AuditLogs.ListAuditLogs;

public interface IListAuditLogsQuery
{
    Task<PagedResult<AuditLogItemDto>> ExecuteAsync(ListAuditLogsRequest request, CancellationToken ct = default);
}
