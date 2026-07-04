namespace Kyvo.Application.Ports.Email;

public interface IEmailService
{
    Task SendInviteAsync(
        string toEmail,
        string tenantName,
        string inviteToken,
        string acceptPath,
        CancellationToken ct = default);
}
