using Cvitlo.Documents.PublicApi;
using Cvitlo.Documents.PublicApi.Resumes;
using Microsoft.AspNetCore.Components;

namespace Cvitlo.Api.IntegrationTests;

// Replaces the Playwright PDF renderer in the rate limiting fixture. Hold() blocks every later render, so a
// request can be parked inside the renderer holding its expensive-resource permit while the next is sent,
// instead of racing on wall-clock render time.
public sealed class GatedDocumentsModuleApi : IDocumentsModuleApi
{
    private static readonly byte[] StubPdf = [0x25, 0x50, 0x44, 0x46];
    private const string StubHtml = "<html><body>stub</body></html>";

    private TaskCompletionSource? _entered;
    private TaskCompletionSource? _release;

    public Task Entered => _entered?.Task ?? Task.CompletedTask;

    public async Task<Stream> RenderResumeToPdfAsync(ResumeDocument resume, ResumeTemplate template, CancellationToken ct)
    {
        await PassGateAsync(ct);

        return new MemoryStream(StubPdf);
    }

    public string TemplateVersion(ResumeTemplate template) => "stub";

    private async Task PassGateAsync(CancellationToken ct)
    {
        var release = _release;

        if (release is not null)
        {
            _entered?.TrySetResult();

            await release.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        }
    }

    // Deliberately ungated: the gate parks a request in the expensive-resource limiter, which guards only PDF
    // generation, while this overload also serves email bodies.
    public Task<string> RenderToHtmlAsync<TComponent>(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct)
        where TComponent : IComponent
        => Task.FromResult(StubHtml);

    public Gate Hold()
    {
        _entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        return new Gate(this);
    }

    public sealed class Gate(GatedDocumentsModuleApi api) : IDisposable
    {
        public void Dispose()
        {
            api._release?.TrySetResult();
            api._release = null;
        }
    }
}
