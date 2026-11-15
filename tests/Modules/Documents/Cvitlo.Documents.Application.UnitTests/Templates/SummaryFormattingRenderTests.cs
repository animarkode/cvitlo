using Cvitlo.Documents.PublicApi.Resumes;
using Shouldly;

namespace Cvitlo.Documents.Application.UnitTests.Templates;

// Guards the end-to-end render contract: Tiptap formatting stored on the resume
// must survive Razor rendering (MarkupString + render-time Sanitize) and reach the
// PDF HTML intact, so Chromium can style it. Renders the template to HTML only —
// no browser/Testcontainers needed.
public class SummaryFormattingRenderTests
{
    [Fact]
    public async Task RenderSummary_FormattingTags_ShouldSurviveIntoPdfHtml()
    {
        var resume = new ResumeDocument
        {
            Localization = "en-us",
            Summary = "<p><strong>bold</strong> <em>italic</em> <u>under</u> <s>strike</s></p>"
                      + "<h2>Heading two</h2><h3>Heading three</h3>"
                      + "<p style=\"text-align: center\">centered</p>"
                      + "<ul><li><p>one</p></li><li><p>two</p></li></ul>"
                      + "<ol><li><p>first</p></li></ol>",
        };

        var html = await TemplateRenderer.RenderAsync(resume);

        html.ShouldContain("<strong>bold</strong>");
        html.ShouldContain("<em>italic</em>");
        html.ShouldContain("<u>under</u>");
        html.ShouldContain("<s>strike</s>");
        html.ShouldContain("<h2>Heading two</h2>");
        html.ShouldContain("<h3>Heading three</h3>");
        html.ShouldContain("text-align: center");
        html.ShouldContain("<ul>");
        html.ShouldContain("<ol>");
    }
}
