using Kyvo.Domain.Interfaces;

namespace Kyvo.Domain.Common;

public abstract class BaseEntity : IHasTimestamps
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void SetCreatedAt()
    {
        var utcNow = DateTime.UtcNow;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;

    /// <summary>Restores identifier and timestamps when rehydrating from persistence.</summary>
    protected void RestoreIdentity(Guid id, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
