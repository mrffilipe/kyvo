using Kyvo.Domain.Common;

namespace Kyvo.Domain.Interfaces;

/// <summary>
/// Implemented by entities that track creation/update timestamps via <see cref="BaseEntity"/>.
/// </summary>
public interface IHasTimestamps
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }

    void SetCreatedAt();
    void SetUpdatedAt();
}
