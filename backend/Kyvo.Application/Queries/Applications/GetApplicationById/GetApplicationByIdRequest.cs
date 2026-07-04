namespace Kyvo.Application.Queries.Applications.GetApplicationById;

public sealed record GetApplicationByIdRequest
{
    public required Guid ApplicationId { get; init; }
}
