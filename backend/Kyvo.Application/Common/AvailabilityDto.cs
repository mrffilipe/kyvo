namespace Kyvo.Application.Common;

public sealed record AvailabilityDto
{
    public required bool Available { get; init; }
}
