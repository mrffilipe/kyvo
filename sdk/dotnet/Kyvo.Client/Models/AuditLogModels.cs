namespace Kyvo.Client.Models;

public sealed record AuditLogItemDto(
    Guid Id,
    Guid TenantId,
    Guid? UserId,
    Guid? MembershipId,
    string Action,
    string ResourceType,
    Guid? ResourceId,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt);

public sealed record ListAuditLogsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null,
    string? Action = null,
    string? ResourceType = null,
    DateTime? From = null,
    DateTime? To = null);

public sealed record AuditLogFilterOptionDto(string Value);

public sealed record ListAuditLogFilterOptionsQuery(
    string Field,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);
