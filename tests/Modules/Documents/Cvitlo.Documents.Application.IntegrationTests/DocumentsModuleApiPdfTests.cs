using System.Text;
using Cvitlo.Documents.Application.Services;
using Cvitlo.Documents.PublicApi;
using Cvitlo.Documents.PublicApi.Resumes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cvitlo.Documents.Application.IntegrationTests;

// Needs the Playwright Chromium shell: pwsh <output>/playwright.ps1 install --only-shell
public class DocumentsModuleApiPdfTests
{
    [Fact]
    public async Task RenderResumeToPdf_RealBrowser_ShouldReturnPdfDocument()
    {
        await using var provider = new ServiceCollection().AddLogging().AddDocumentsModule().BuildServiceProvider();
        await provider.GetRequiredService<PlaywrightService>().InitBrowserAsync();
        await using var scope = provider.CreateAsyncScope();
        var api = scope.ServiceProvider.GetRequiredService<IDocumentsModuleApi>();
        var resume = new ResumeDocument
        {
            Localization = "en-us",
            Summary = "<p>Seasoned engineer</p>",
            Person = new PersonDocument { Firstname = "Ada", Lastname = "Lovelace", JobTitle = "Analyst" }
        };

        await using var pdf = await api.RenderResumeToPdfAsync(resume, ResumeTemplate.First, TestContext.Current.CancellationToken);

        using var buffer = new MemoryStream();
        await pdf.CopyToAsync(buffer, TestContext.Current.CancellationToken);
        buffer.Length.ShouldBeGreaterThan(1024);
        Encoding.ASCII.GetString(buffer.ToArray(), 0, 5).ShouldBe("%PDF-");
    }
}
