using Cvitlo.Documents.Application.Razor;
using Cvitlo.Documents.Application.Templates;
using Cvitlo.Documents.PublicApi;
using Cvitlo.Documents.PublicApi.Resumes;
using Microsoft.AspNetCore.Components;

namespace Cvitlo.Documents.Application;

public class DocumentsModuleApi(IRender render) : IDocumentsModuleApi
{
    public async Task<Stream> RenderResumeToPdfAsync(
        ResumeDocument resume,
        ResumeTemplate template,
        CancellationToken ct)
    {
        var html = RenderResumeHtmlAsync(resume, template, ct);

        return await render.ToPdfAsync(async () => await html, ct);
    }

    public string TemplateVersion(ResumeTemplate template)
    {
        return TemplateComponent(template).Assembly.ManifestModule.ModuleVersionId.ToString();
    }

    public async Task<Stream> RenderToPdfAsync<TComponent>(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct)
        where TComponent : IComponent
    {
        var html = render.RazorToHtmlAsync<TComponent>(parameters, ct);
        var stream = await render.ToPdfAsync(async () => await html, ct);

        return stream;
    }

    public async Task<string> RenderToHtmlAsync<TComponent>(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct)
        where TComponent : IComponent
    {
        return await render.RazorToHtmlAsync<TComponent>(parameters, ct);
    }

    private Task<string> RenderResumeHtmlAsync(ResumeDocument resume, ResumeTemplate template, CancellationToken ct)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal) { ["Resume"] = resume };

        return template switch
        {
            ResumeTemplate.First => render.RazorToHtmlAsync<FirstTemplate>(parameters, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(template), template, null)
        };
    }

    private static Type TemplateComponent(ResumeTemplate template)
    {
        return template switch
        {
            ResumeTemplate.First => typeof(FirstTemplate),
            _ => throw new ArgumentOutOfRangeException(nameof(template), template, null)
        };
    }
}
