namespace Kyvo.Client.Models;

public sealed record TenantRoleDto(
    Guid Id,
    Guid TenantId,
    string Key,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive);

public sealed record CreateTenantRoleBody(string Key, string Name, string? Description = null);

public sealed record UpdateTenantRoleBody(string Name, string? Description, bool IsActive);
