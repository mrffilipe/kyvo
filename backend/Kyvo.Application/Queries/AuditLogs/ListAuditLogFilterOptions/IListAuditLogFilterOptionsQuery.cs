using Kyvo.Application.Common;
using Kyvo.Application.Queries.AuditLogs.Dtos;

namespace Kyvo.Application.Queries.AuditLogs.ListAuditLogFilterOptions;

public interface IListAuditLogFilterOptionsQuery
{
    Task<PagedResult<AuditLogFilterOptionDto>> ExecuteAsync(ListAuditLogFilterOptionsRequest request, CancellationToken ct = default);
}
