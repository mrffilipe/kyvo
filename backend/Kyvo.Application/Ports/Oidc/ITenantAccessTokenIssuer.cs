using Kyvo.Domain.Entities;

namespace Kyvo.Application.Ports.Oidc;

public interface ITenantAccessTokenIssuer
{
    string IssueToken(AuthSession session, IEnumerable<string> platformRoles, IEnumerable<string> tenantRoles);
}
