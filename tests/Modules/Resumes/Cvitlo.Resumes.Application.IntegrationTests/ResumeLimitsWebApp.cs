using Microsoft.AspNetCore.Hosting;

namespace Cvitlo.Resumes.Application.IntegrationTests;

/// <summary>
/// Runs the API with finite resume limits (regular 1, premium 2); <see cref="WebApp"/> keeps both at
/// <c>int.MaxValue</c>, so the rejecting branch of the creation policy is only reachable here.
/// </summary>
public class ResumeLimitsWebApp : WebApp
{
    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        base.ConfigureApp(builder);

        builder.UseSetting("ResumeLimits:Limits:Regular", "1");
        builder.UseSetting("ResumeLimits:Limits:Premium", "2");
    }
}
