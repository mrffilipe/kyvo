using Kyvo.Application.Common;

namespace Kyvo.Application.Services.Users;

public interface IUserService
{
    Task<Guid> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task UpdateProfileAsync(UpdateUserProfileRequest request, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(GetUserByIdRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<UserMembershipDto>> ListMembershipsAsync(ListUserMembershipsRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<UserPickerDto>> SearchAsync(SearchUsersRequest request, CancellationToken cancellationToken = default);
    Task LinkExternalIdentityAsync(LinkExternalIdentityRequest request, CancellationToken cancellationToken = default);
}
