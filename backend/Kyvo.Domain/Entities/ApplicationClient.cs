using Kyvo.Domain.Common;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.Entities;

public sealed class ApplicationClient : BaseEntity
{
    public Guid ApplicationId { get; private set; }
    public Application Application { get; private set; } = null!;

    public string ClientId { get; private set; } = default!;
    public ClientType ClientType { get; private set; }
    public ICollection<string> RedirectUris { get; private set; } = [];
    public ICollection<string> PostLogoutRedirectUris { get; private set; } = [];
    public ICollection<string> AllowedScopes { get; private set; } = [];
    public int AccessTokenTtlSeconds { get; private set; }

    /// <summary>
    /// Indicates that this client is managed by the platform and cannot be edited or removed via API.
    /// Example: the admin console client created automatically during bootstrap.
    /// </summary>
    public bool IsSystem { get; private set; }

    private ApplicationClient()
    {
    }

    public ApplicationClient(
        Guid applicationId,
        string clientId,
        ClientType clientType,
        IReadOnlyList<string> redirectUris,
        IReadOnlyList<string> allowedScopes,
        int accessTokenTtlSeconds,
        IReadOnlyList<string>? postLogoutRedirectUris = null,
        bool isSystem = false)
    {
        if (applicationId == Guid.Empty || string.IsNullOrWhiteSpace(clientId))
        {
            throw new DomainValidationException(DomainErrorMessages.ApplicationClient.DATA_INVALID);
        }

        ApplicationId = applicationId;
        ClientId = clientId.Trim();
        ClientType = clientType;
        RedirectUris = redirectUris.ToList();
        PostLogoutRedirectUris = postLogoutRedirectUris?.ToList() ?? [];
        AllowedScopes = allowedScopes.ToList();
        AccessTokenTtlSeconds = accessTokenTtlSeconds > 0 ? accessTokenTtlSeconds : 900;
        IsSystem = isSystem;
    }
}
