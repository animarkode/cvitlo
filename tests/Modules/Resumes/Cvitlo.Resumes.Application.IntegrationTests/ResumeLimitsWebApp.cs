using Microsoft.AspNetCore.Hosting;

namespace Cvitlo.Resumes.Application.IntegrationTests;

// WebApp keeps both resume limits at int.MaxValue, so the rejecting branch of the creation
// policy is reachable only with these finite limits.
public class ResumeLimitsWebApp : WebApp
{
    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        base.ConfigureApp(builder);

        builder.UseSetting("ResumeLimits:Limits:Regular", "1");
        builder.UseSetting("ResumeLimits:Limits:Premium", "2");
    }
}
