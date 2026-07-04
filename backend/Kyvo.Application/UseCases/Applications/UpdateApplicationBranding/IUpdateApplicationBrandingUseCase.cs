namespace Kyvo.Application.UseCases.Applications.UpdateApplicationBranding;

public interface IUpdateApplicationBrandingUseCase
{
    Task ExecuteAsync(UpdateApplicationBrandingRequest request, CancellationToken ct = default);
}
