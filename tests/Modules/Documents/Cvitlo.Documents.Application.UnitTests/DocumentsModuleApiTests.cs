using Cvitlo.Documents.Application.Templates;
using Cvitlo.Documents.Application.UnitTests.Templates;
using Cvitlo.Documents.PublicApi;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cvitlo.Documents.Application.UnitTests;

public class DocumentsModuleApiTests
{
    [Fact]
    public async Task RenderToHtml_RegisteredModule_ShouldRenderTheTemplate()
    {
        await using var provider = new ServiceCollection().AddLogging().AddDocumentsModule().BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var api = scope.ServiceProvider.GetRequiredService<IDocumentsModuleApi>();
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal) { ["Resume"] = ResumeBuilder.Full() };

        var html = await api.RenderToHtmlAsync<FirstTemplate>(parameters, TestContext.Current.CancellationToken);

        html.ShouldContain("Ada");
    }

    [Fact]
    public async Task RenderToHtml_UnknownParameter_ShouldThrowInvalidOperation()
    {
        await using var provider = new ServiceCollection().AddLogging().AddDocumentsModule().BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var api = scope.ServiceProvider.GetRequiredService<IDocumentsModuleApi>();
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal) { ["Nope"] = 1 };

        await Should.ThrowAsync<InvalidOperationException>(
            () => api.RenderToHtmlAsync<FirstTemplate>(parameters, TestContext.Current.CancellationToken));
    }
}
