namespace Kyvo.Application.Services.Security;

public interface IInviteTokenProtector
{
    string Protect(string plaintextToken);

    string Unprotect(string protectedToken);
}
