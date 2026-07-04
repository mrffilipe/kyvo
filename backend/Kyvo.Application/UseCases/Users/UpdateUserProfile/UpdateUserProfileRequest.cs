namespace Kyvo.Application.UseCases.Users.UpdateUserProfile;

public sealed record UpdateUserProfileRequest
{
    public required Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public string? PhotoUrl { get; init; }
}
