using System.Text.Json.Serialization;

namespace Kyvo.Application.UseCases.Invites.AcceptInvite;

public sealed record AcceptInviteRequest
{
    [JsonPropertyName("token")]
    public required string InviteToken { get; init; }

    public Guid ActorUserId { get; init; }
}
