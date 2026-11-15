using Cvitlo.Documents.Application.Razor;
using Cvitlo.Documents.Application.Services;
using Cvitlo.Documents.Application.Templates;
using Cvitlo.Documents.PublicApi.Resumes;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cvitlo.Documents.Application.UnitTests;

internal static class TemplateRenderer
{
    // PlaywrightService is never started: only the HTML path is exercised, so no browser is needed.
    public static async Task<string> RenderAsync(ResumeDocument? resume)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var htmlRenderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        await using var playwrightService = new PlaywrightService();
        var render = new Render(htmlRenderer, playwrightService);

        return await render.RazorToHtmlAsync<FirstTemplate>(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["Resume"] = resume },
            TestContext.Current.CancellationToken);
    }
}
