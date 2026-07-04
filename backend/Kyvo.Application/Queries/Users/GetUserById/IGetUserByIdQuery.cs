using Kyvo.Application.Queries.Users.Dtos;

namespace Kyvo.Application.Queries.Users.GetUserById;

public interface IGetUserByIdQuery
{
    Task<UserDto?> ExecuteAsync(GetUserByIdRequest request, CancellationToken ct = default);
}
