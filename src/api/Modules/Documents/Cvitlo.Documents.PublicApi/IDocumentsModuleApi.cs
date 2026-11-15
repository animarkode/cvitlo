using Cvitlo.Documents.PublicApi.Resumes;
using Microsoft.AspNetCore.Components;

namespace Cvitlo.Documents.PublicApi;

public interface IDocumentsModuleApi
{
    Task<Stream> RenderResumeToPdfAsync(ResumeDocument resume, ResumeTemplate template, CancellationToken ct);

    /// <summary>Identifies the compiled template, so a cache of its output can be dropped when the template changes.</summary>
    string TemplateVersion(ResumeTemplate template);

    Task<Stream> RenderToPdfAsync<TComponent>(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct)
        where TComponent : IComponent;

    /// <summary>
    /// Renders a Razor component to an HTML string, for use in documents and email bodies alike.
    /// </summary>
    /// <typeparam name="TComponent">The Razor component to render.</typeparam>
    /// <param name="parameters">The parameters to pass to the component.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The rendered HTML markup.</returns>
    Task<string> RenderToHtmlAsync<TComponent>(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct)
        where TComponent : IComponent;
}