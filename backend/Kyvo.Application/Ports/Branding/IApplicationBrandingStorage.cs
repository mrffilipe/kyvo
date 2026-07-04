namespace Kyvo.Application.Ports.Branding;

public interface IApplicationBrandingStorage
{
    Task<string> SaveLogoAsync(
        Guid applicationId,
        Stream content,
        string contentType,
        string fileName,
        CancellationToken ct = default);

    Task DeleteLogoAsync(Guid applicationId, CancellationToken ct = default);
}
