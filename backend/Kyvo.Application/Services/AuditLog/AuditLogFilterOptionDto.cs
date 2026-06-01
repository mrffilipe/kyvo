namespace Kyvo.Application.Services.AuditLog;

public sealed record AuditLogFilterOptionDto
{
    public required string Value { get; init; }
}
