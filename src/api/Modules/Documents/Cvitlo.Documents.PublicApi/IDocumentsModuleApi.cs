using Cvitlo.Documents.PublicApi.Resumes;
using Microsoft.AspNetCore.Components;

namespace Cvitlo.Documents.PublicApi;

public interface IDocumentsModuleApi
{
    Task<Stream> RenderResumeToPdfAsync(ResumeDocument resume, ResumeTemplate template, CancellationToken ct);

    /// <summary>Identifies the compiled template, so a cache of its output can be dropped when the template changes.</summary>
    string TemplateVersion(ResumeTemplate template);

    /// <summary>Renders a Razor component to HTML, for documents and email bodies alike.</summary>
    Task<string> RenderToHtmlAsync<TComponent>(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct)
        where TComponent : IComponent;
}