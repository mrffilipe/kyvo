namespace Kyvo.Application.Queries.AuditLogs.Dtos;

public sealed record AuditLogFilterOptionDto
{
    public required string Value { get; init; }
}
