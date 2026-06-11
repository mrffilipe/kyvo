using Kyvo.Application.Common;

namespace Kyvo.Application.Services.AuditLog;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogItemDto>> ListAuditLogsAsync(ListAuditLogsRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLogFilterOptionDto>> ListFilterOptionsAsync(ListAuditLogFilterOptionsRequest request, CancellationToken cancellationToken = default);
}
