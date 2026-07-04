using Kyvo.API.Common;
using Kyvo.Application.Common;
using Kyvo.Application.Queries.Users.GetUserById;
using Kyvo.Application.Queries.Users.ListUserMemberships;
using Kyvo.Application.Queries.Users.SearchUsers;
using Kyvo.Application.UseCases.Users.UpdateUserProfile;
using Kyvo.Application.Services.UserScope;
using Kyvo.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Kyvo.Application.Queries.Users.Dtos;

namespace Kyvo.API.Controllers;

/// <summary>
/// Current user profile and membership listing.
/// </summary>
public sealed class UsersController : V1ApiControllerBase
{
    private readonly IUserScope _userScope;
    private readonly ISearchUsersQuery _searchUsersQuery;
    private readonly IGetUserByIdQuery _getUserByIdQuery;
    private readonly IListUserMembershipsQuery _listUserMembershipsQuery;
    private readonly IUpdateUserProfileUseCase _updateUserProfileUseCase;

    public UsersController(
        IUserScope userScope,
        ISearchUsersQuery searchUsersQuery,
        IGetUserByIdQuery getUserByIdQuery,
        IListUserMembershipsQuery listUserMembershipsQuery,
        IUpdateUserProfileUseCase updateUserProfileUseCase)
    {
        _userScope = userScope;
        _searchUsersQuery = searchUsersQuery;
        _getUserByIdQuery = getUserByIdQuery;
        _listUserMembershipsQuery = listUserMembershipsQuery;
        _updateUserProfileUseCase = updateUserProfileUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserPickerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<UserPickerDto>>> SearchUsers([FromQuery] SearchUsersRequest request, CancellationToken ct)
    {
        var canSearch = _userScope.HasAnyPlatformRole(PlatformRoleDefaults.PLATFORM_ADMINISTRATOR)
            || _userScope.HasAnyTenantRole(TenantRoleDefaults.OWNER, TenantRoleDefaults.ADMIN);

        if (!canSearch)
        {
            return Forbid();
        }

        var result = await _searchUsersQuery.ExecuteAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken ct)
    {
        var user = await _getUserByIdQuery.ExecuteAsync(
            new GetUserByIdRequest { UserId = _userScope.UserId },
            ct);

        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("me/memberships")]
    [ProducesResponseType(typeof(PagedResult<UserMembershipDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserMembershipDto>>> ListUserMemberships([FromQuery] ListUserMembershipsRequest request, CancellationToken ct)
    {
        var result = await _listUserMembershipsQuery.ExecuteAsync(
            request with { UserId = _userScope.UserId },
            ct);

        return Ok(result);
    }

    [HttpPatch("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserProfileRequest request, CancellationToken ct)
    {
        await _updateUserProfileUseCase.ExecuteAsync(
            request with { UserId = _userScope.UserId },
            ct);

        return NoContent();
    }
}
