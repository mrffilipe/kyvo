using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.UseCases.Applications.CreateApplication;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Applications;

public sealed class CreateApplicationUseCase : ICreateApplicationUseCase
{
    private readonly IApplicationRepository _applications;
    private readonly IUnitOfWork _unitOfWork;

    public CreateApplicationUseCase(IApplicationRepository applications, IUnitOfWork unitOfWork)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> ExecuteAsync(CreateApplicationRequest request, CancellationToken ct = default)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await _applications.SlugAlreadyExistsAsync(slug, ct))
        {
            throw new DomainBusinessRuleException(ApplicationErrorMessages.Application.SLUG_ALREADY_EXISTS);
        }

        var application = new Domain.Entities.Application(request.Name, slug, request.Type);
        await _applications.AddAsync(application, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return application.Id;
    }
}
