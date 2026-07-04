namespace Kyvo.Application.Services.Security;

/// <summary>
/// One-way hash for tenant invite tokens stored in <see cref="Kyvo.Domain.Entities.TenantInvite"/>.
/// </summary>
public interface IInviteTokenHasher
{
    string Hash(string token);
}
