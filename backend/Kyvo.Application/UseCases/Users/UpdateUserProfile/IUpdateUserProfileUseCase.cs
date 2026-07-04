using Kyvo.Application.UseCases.Users.UpdateUserProfile;

namespace Kyvo.Application.UseCases.Users.UpdateUserProfile;

public interface IUpdateUserProfileUseCase
{
    Task ExecuteAsync(UpdateUserProfileRequest request, CancellationToken ct = default);
}
