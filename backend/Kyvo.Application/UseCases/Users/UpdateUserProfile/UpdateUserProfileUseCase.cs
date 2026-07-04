using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Users.UpdateUserProfile;

public sealed class UpdateUserProfileUseCase : IUpdateUserProfileUseCase
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserProfileUseCase(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(UpdateUserProfileRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetForUpdateAsync(request.UserId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.User.USER_NOT_FOUND);

        user.UpdateProfile(request.DisplayName, request.PhotoUrl);
        await _users.SyncFromDomainAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
