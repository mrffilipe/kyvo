namespace Kyvo.Application.Services.AppService;

public interface IApplicationBrandingStorage
{
    Task<string> SaveLogoAsync(
        Guid applicationId,
        Stream content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteLogoAsync(Guid applicationId, CancellationToken cancellationToken = default);
}
