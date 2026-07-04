namespace Kyvo.Application.Common;

public static class InviteAcceptPath
{
    public static string Build(string rawToken) =>
        $"/accept-invite?token={Uri.EscapeDataString(rawToken)}";
}
