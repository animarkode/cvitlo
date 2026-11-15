namespace Cvitlo.Api.IntegrationTests.Features.RateLimiting;

public static class RateLimitingTestConstants
{
    public const string UserUri = "api/v1/auth/user";
    public const string LoginUri = "api/v1/auth/login";
    public const string LogoutUri = "api/v1/auth/logout";
    public const string RegisterUri = "api/v1/auth/register";
    public const string CallbackUri = "api/v1/auth/callback";
    public const string BackchannelLogoutUri = "api/v1/auth/backchannel-logout";

    public const string ForwardedForHeader = "X-Forwarded-For";

    public const string TrustedProxyIp = "10.10.10.10";
    public const string UntrustedProxyIp = "10.99.99.99";

    // Must match the WebApp fixture's RateLimiting:GlobalAnonymous PermitLimit setting.
    public const int AnonymousPermitLimit = 3;

    // Must match the WebApp fixture's RateLimiting:GlobalAuthenticated PermitLimit setting.
    // Held well above every per-user named policy so a 429 from one of those is never masked by
    // the global baseline.
    public const int AuthenticatedPermitLimit = 30;

    // Must match the WebApp fixture's RateLimiting:AuthExternal:PermitLimit. Below AnonymousPermitLimit so a 429
    // at limit + 1 proves the endpoint policy, not the global baseline.
    public const int AuthExternalPermitLimit = 2;

    // Must match the WebApp fixture's RateLimiting:AuthSession:PermitLimit. Below AnonymousPermitLimit so a 429
    // at limit + 1 proves the endpoint policy, not the global baseline.
    public const int AuthSessionPermitLimit = 2;

    // Must match the WebApp fixture's RateLimiting:GlobalAnonymous/GlobalAuthenticated WindowSeconds/SegmentsPerWindow settings.
    public const int TestWindowSeconds = 60;
    public const int TestSegmentsPerWindow = 1;

    // Must match the WebApp fixture's RateLimiting:AuthenticatedDefault:PermitLimit. Far below
    // AuthenticatedPermitLimit so a 429 at limit + 1 proves the endpoint policy, not the global baseline.
    public const int AuthenticatedDefaultPermitLimit = 2;

    public const string ResumesUri = "api/v1/resumes";
    public const string ResumeEducationsUriFormat = "api/v1/resumes/{0}/educations";
    public const string ResumeGenerateUriFormat = "api/v1/resumes/{0}/generate";
    public const string ResumeDownloadUriFormat = "api/v1/resumes/{0}/download";

    public const string ExpectedTitle = "Too Many Requests";
    public const string ExpectedDetail = "Rate limit exceeded. Please retry later.";
}
