using Cvitlo.Common.Testing.Hosting;
using Cvitlo.Documents.PublicApi;
using Cvitlo.Documents.PublicApi.Resumes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Cvitlo.Resumes.Application.IntegrationTests;

public class WebApp : CvitloWebApp
{
    private const int PremiumUsers = 2;

    public CollectingLoggerProvider Logs { get; } = new();

    // When false, no Redis container is built or started, so a fixture that never exercises Redis
    // against a real instance (e.g. RedisUnavailableWebApp) doesn't pay for one.
    protected override bool UsesRedis => true;

    protected override async Task SeedUsersAsync(CancellationToken ct)
    {
        for (var i = 0; i < Users.Count; i++)
        {
            var userId = await Keycloak.Admin.CreateUserAsync(Users[i], ct: ct);
            if (i < PremiumUsers)
            {
                await Keycloak.Admin.AssignRealmRoleAsync(userId, "premium", ct);
            }
        }
    }

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        base.ConfigureApp(builder);

        builder.ConfigureServices(services =>
        {
            var documentsModuleApiMock = Substitute.For<IDocumentsModuleApi>();

            documentsModuleApiMock
                .RenderResumeToPdfAsync(
                    Arg.Any<ResumeDocument>(),
                    ResumeTemplate.First,
                    Arg.Any<CancellationToken>())
                .Returns(_ => new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }));

            documentsModuleApiMock.TemplateVersion(ResumeTemplate.First).Returns("test-version");

            services.AddScoped<IDocumentsModuleApi>(_ => documentsModuleApiMock);
        });

        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }
}
