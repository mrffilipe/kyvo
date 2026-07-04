namespace Kyvo.Application.Queries.Tenants.Dtos;

public sealed record TenantDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Key { get; init; }
}
