namespace Kyvo.Application.Services.AppService;

public sealed record UploadApplicationBrandingLogoRequest
{
    public required Guid ApplicationId { get; init; }
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
    public required IReadOnlyList<string> ActorPlatformRoles { get; init; }
}
