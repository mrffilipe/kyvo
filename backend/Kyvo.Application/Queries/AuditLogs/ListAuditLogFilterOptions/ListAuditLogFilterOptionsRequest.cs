using Kyvo.Application.Common;

namespace Kyvo.Application.Queries.AuditLogs.ListAuditLogFilterOptions;

public sealed record ListAuditLogFilterOptionsRequest : PagedRequest
{
    public required string Field { get; init; }
    public string? Search { get; init; }
}
