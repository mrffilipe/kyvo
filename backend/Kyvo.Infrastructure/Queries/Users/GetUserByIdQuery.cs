using Kyvo.Application.Queries.Users.GetUserById;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Kyvo.Application.Queries.Users.Dtos;

namespace Kyvo.Infrastructure.Queries.Users;

public sealed class GetUserByIdQuery : IGetUserByIdQuery
{
    private readonly ApplicationDbContext _context;

    public GetUserByIdQuery(ApplicationDbContext context) => _context = context;

    public async Task<UserDto?> ExecuteAsync(GetUserByIdRequest request, CancellationToken ct = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(x => x.Memberships)
            .ThenInclude(x => x.Tenant)
            .Include(x => x.Memberships)
            .ThenInclude(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == request.UserId, ct);

        if (user is null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            PhotoUrl = user.PhotoUrl,
            Memberships = user.Memberships
                .Where(x => x.IsActive)
                .Select(x => new UserMembershipDto
                {
                    MembershipId = x.Id,
                    TenantId = x.TenantId,
                    TenantName = x.Tenant.Name,
                    TenantKey = x.Tenant.Key.Value,
                    Roles = x.Roles.Select(role => role.Role.Key.Value).ToList()
                })
                .ToList()
        };
    }
}
