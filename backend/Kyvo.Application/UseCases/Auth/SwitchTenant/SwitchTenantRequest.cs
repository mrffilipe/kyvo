namespace Kyvo.Application.UseCases.Auth.SwitchTenant;

public sealed record SwitchTenantRequest
{
    public required Guid TenantId { get; init; }
}
