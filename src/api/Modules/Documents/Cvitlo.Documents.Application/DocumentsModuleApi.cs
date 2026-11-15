using Cvitlo.Documents.Application.Razor;
using Cvitlo.Documents.Application.Templates;
using Cvitlo.Documents.PublicApi;
using Cvitlo.Documents.PublicApi.Resumes;
using Microsoft.AspNetCore.Components;

namespace Cvitlo.Documents.Application;

public class DocumentsModuleApi(IRender render) : IDocumentsModuleApi
{
    // Every template is compiled into this assembly, so its build id changes whenever any template does.
    private static readonly string TemplatesBuild = typeof(DocumentsModuleApi).Assembly.ManifestModule.ModuleVersionId.ToString();

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
        if (!Enum.IsDefined(template))
        {
            throw new ArgumentOutOfRangeException(nameof(template), template, null);
        }

        return TemplatesBuild;
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
}
