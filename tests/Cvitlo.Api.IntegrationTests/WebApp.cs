using Cvitlo.Api.IntegrationTests.Features.RateLimiting.Data;
using Cvitlo.Common.Testing.Hosting;
using Cvitlo.Documents.PublicApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cvitlo.Api.IntegrationTests;

public class WebApp : CvitloWebApp
{
    protected override async Task SeedUsersAsync(CancellationToken ct)
    {
        await base.SeedUsersAsync(ct);

        foreach (var user in RateLimitingUsersData.Generate())
        {
            await Keycloak.Admin.CreateUserAsync(user, ct: ct);
        }
    }

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        base.ConfigureApp(builder);

        builder.ConfigureServices(services =>
        {
            services.AddTransient<IStartupFilter, TestClientIpStartupFilter>();
            services.AddTransient<IStartupFilter, TestThrowingRoutesStartupFilter>();
            services.AddSingleton<GatedDocumentsModuleApi>();
            services.AddScoped<IDocumentsModuleApi>(sp => sp.GetRequiredService<GatedDocumentsModuleApi>());
        });

        builder.UseSetting("RateLimiting:Enabled", "true");
        builder.UseSetting("RateLimiting:GlobalAnonymous:PermitLimit", "3");
        builder.UseSetting("RateLimiting:GlobalAnonymous:WindowSeconds", "60");
        builder.UseSetting("RateLimiting:GlobalAnonymous:SegmentsPerWindow", "1");
        builder.UseSetting("RateLimiting:GlobalAuthenticated:PermitLimit", "30");
        builder.UseSetting("RateLimiting:GlobalAuthenticated:WindowSeconds", "60");
        builder.UseSetting("RateLimiting:GlobalAuthenticated:SegmentsPerWindow", "1");
        builder.UseSetting("RateLimiting:AuthExternal:PermitLimit", "2");
        builder.UseSetting("RateLimiting:AuthExternal:WindowSeconds", "60");
        builder.UseSetting("RateLimiting:AuthExternal:SegmentsPerWindow", "1");
        builder.UseSetting("RateLimiting:AuthSession:PermitLimit", "2");
        builder.UseSetting("RateLimiting:AuthSession:WindowSeconds", "60");
        builder.UseSetting("RateLimiting:AuthSession:SegmentsPerWindow", "1");
        builder.UseSetting("RateLimiting:AuthenticatedDefault:PermitLimit", "2");
        builder.UseSetting("RateLimiting:AuthenticatedDefault:WindowSeconds", "60");
        builder.UseSetting("RateLimiting:AuthenticatedDefault:SegmentsPerWindow", "1");

        // A concurrency limiter of one permit and no queue makes the expensive-resource rejection
        // deterministic: park one render inside GatedDocumentsModuleApi, and the very next document
        // request is refused immediately instead of waiting in a queue.
        builder.UseSetting("RateLimiting:ExpensiveResource:PermitLimit", "1");
        builder.UseSetting("RateLimiting:ExpensiveResource:QueueLimit", "0");

        // Pinned rather than inherited from appsettings.json so a resume-limit change can never
        // silently break the document tests, which each need exactly one creatable resume.
        builder.UseSetting("ResumeLimits:Limits:Regular", "1");

        builder.UseSetting("ForwardedHeaders:KnownProxies:0", "10.10.10.10");
    }
}
