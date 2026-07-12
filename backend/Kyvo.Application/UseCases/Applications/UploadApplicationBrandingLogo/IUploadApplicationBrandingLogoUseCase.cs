namespace Kyvo.Application.UseCases.Applications.UploadApplicationBrandingLogo;

public interface IUploadApplicationBrandingLogoUseCase
{
    Task<string> ExecuteAsync(UploadApplicationBrandingLogoRequest request, CancellationToken ct = default);
}
