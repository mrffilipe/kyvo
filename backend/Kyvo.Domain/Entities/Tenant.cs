using Kyvo.Domain.Common;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.ValueObjects;

namespace Kyvo.Domain.Entities;

public sealed class Tenant : BaseEntity
{
    public string Name { get; private set; } = default!;
    public TenantKey Key { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public ICollection<TenantMembership> Memberships { get; private set; } = [];
    public ICollection<TenantRole> Roles { get; private set; } = [];
    public ICollection<ApplicationTenant> Applications { get; private set; } = [];

    private Tenant()
    {
    }

    public Tenant(string name, TenantKey key)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException(DomainErrorMessages.Tenant.NAME_REQUIRED);
        }

        Name = name.Trim();
        Key = key;
        IsActive = true;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException(DomainErrorMessages.Tenant.NAME_REQUIRED);
        }

        Name = name.Trim();
    }
}
