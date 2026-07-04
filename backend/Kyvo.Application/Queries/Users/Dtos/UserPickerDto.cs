namespace Kyvo.Application.Queries.Users.Dtos;

public sealed record UserPickerDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public string? PhotoUrl { get; init; }
}
