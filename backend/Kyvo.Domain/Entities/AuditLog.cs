using Kyvo.Domain.Common;

namespace Kyvo.Domain.Entities;

public sealed class AuditLog : TenantEntity
{
    public Guid? UserId { get; private set; }
    public User? User { get; private set; }

    public Guid? MembershipId { get; private set; }
    public TenantMembership? Membership { get; private set; }

    public string Action { get; private set; } = default!;
    public string ResourceType { get; private set; } = default!;
    public Guid? ResourceId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private AuditLog()
    {
    }

    public AuditLog(
        Guid tenantId,
        Guid? userId,
        Guid? membershipId,
        string action,
        string resourceType,
        Guid? resourceId,
        string? ipAddress,
        string? userAgent) : base(tenantId)
    {
        UserId = userId;
        MembershipId = membershipId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
