using Kyvo.Application.Common;

namespace Kyvo.Application.Services.AuditLog;

public sealed record ListAuditLogFilterOptionsRequest : PagedRequest
{
    public required string Field { get; init; }

    public string? Search { get; init; }
}
