using FastEndpoints.Testing;
using Microsoft.AspNetCore.Hosting;

namespace Cvitlo.Api.IntegrationTests.Features.Health;

public sealed class ProductionHealthWebApp : WebApp
{
    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        base.ConfigureApp(builder);
        builder.UseEnvironment("Production");
    }
}
