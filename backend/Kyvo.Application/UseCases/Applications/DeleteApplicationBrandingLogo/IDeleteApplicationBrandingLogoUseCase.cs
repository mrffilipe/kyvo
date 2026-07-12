namespace Kyvo.Application.UseCases.Applications.DeleteApplicationBrandingLogo;

public interface IDeleteApplicationBrandingLogoUseCase
{
    Task ExecuteAsync(Guid applicationId, IReadOnlyList<string> actorPlatformRoles, CancellationToken ct = default);
}
