using Kyvo.Client.Models;
using PulseCrm.Api.Data;

namespace PulseCrm.Api.Models;

public sealed record OnboardingCompleteResponse(
    Subscription Subscription,
    TenantContextResult IdpTenantContext,
    OidcTokenResponse? Tokens,
    bool RequiresTokenRefresh,
    string Message);
