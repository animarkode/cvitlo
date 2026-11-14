using Cvitlo.Resumes.Application.Features.Resumes.Shared;
using Shouldly;

namespace Cvitlo.Resumes.Application.UnitTests.Features.Resumes.Shared;

public class HtmlContentSanitizerTests
{
    [Fact]
    public void SanitizeBasicFormatting_Null_ShouldReturnNull()
    {
        HtmlContentSanitizer.SanitizeBasicFormatting(null).ShouldBeNull();
    }

    [Theory]
    [InlineData("<p><strong>bold</strong></p>", "bold")]
    [InlineData("<p><em>italic</em></p>", "italic")]
    [InlineData("<p><u>underline</u></p>", "underline")]
    [InlineData("<p><s>strike</s></p>", "strike")]
    [InlineData("<h2>Heading two</h2>", "Heading two")]
    [InlineData("<h3>Heading three</h3>", "Heading three")]
    [InlineData("<ul><li><p>bullet</p></li></ul>", "bullet")]
    [InlineData("<ol><li><p>ordered</p></li></ol>", "ordered")]
    public void SanitizeBasicFormatting_TiptapFormatting_ShouldKeepTagAndContent(string input, string expectedText)
    {
        var result = HtmlContentSanitizer.SanitizeBasicFormatting(input);

        result.ShouldNotBeNull();
        result.ShouldContain(expectedText);
    }

    [Theory]
    [InlineData("<p><strong>bold</strong></p>", "<strong>")]
    [InlineData("<p><em>italic</em></p>", "<em>")]
    [InlineData("<p><s>strike</s></p>", "<s>")]
    [InlineData("<h2>Heading</h2>", "<h2")]
    [InlineData("<ol><li><p>ordered</p></li></ol>", "<ol")]
    public void SanitizeBasicFormatting_TiptapFormatting_ShouldPreserveAllowedTag(string input, string expectedTag)
    {
        HtmlContentSanitizer.SanitizeBasicFormatting(input)
            .ShouldNotBeNull()
            .ShouldContain(expectedTag);
    }

    [Fact]
    public void SanitizeBasicFormatting_Alignment_ShouldKeepTextAlignStyle()
    {
        const string input = "<p style=\"text-align: center\">centered</p>";

        var result = HtmlContentSanitizer.SanitizeBasicFormatting(input);

        result.ShouldNotBeNull();
        result.ShouldContain("text-align");
        result.ShouldContain("center");
    }

    [Fact]
    public void SanitizeBasicFormatting_HttpsLink_ShouldKeepHref()
    {
        const string input = "<p><a href=\"https://example.com\">site</a></p>";

        var result = HtmlContentSanitizer.SanitizeBasicFormatting(input);

        result.ShouldNotBeNull();
        result.ShouldContain("href");
        result.ShouldContain("https://example.com");
    }

    [Fact]
    public void SanitizeBasicFormatting_ScriptTag_ShouldBeStripped()
    {
        // KeepChildNodes preserves user text, but the executable <script> element
        // itself must be removed — leftover text is inert when rendered as markup.
        const string input = "<p>safe</p><script>alert('xss')</script>";

        var result = HtmlContentSanitizer.SanitizeBasicFormatting(input);

        result.ShouldNotBeNull();
        result.ShouldContain("safe");
        result.ShouldNotContain("<script");
    }

    [Fact]
    public void SanitizeBasicFormatting_JavascriptScheme_ShouldBeStripped()
    {
        const string input = "<p><a href=\"javascript:alert(1)\">x</a></p>";

        var result = HtmlContentSanitizer.SanitizeBasicFormatting(input);

        result.ShouldNotBeNull();
        result.ShouldNotContain("javascript:");
    }

    [Fact]
    public void SanitizeBasicFormatting_DisallowedTag_ShouldKeepTextContent()
    {
        const string input = "<div><span>kept text</span></div>";

        var result = HtmlContentSanitizer.SanitizeBasicFormatting(input);

        result.ShouldNotBeNull();
        result.ShouldContain("kept text");
        result.ShouldNotContain("<span");
        result.ShouldNotContain("<div");
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("<p>abc</p>", 3)]
    [InlineData("<p>a</p><p>b</p>", 2)]
    [InlineData("a &amp; b", 5)]
    [InlineData("<script>xx</script>", 2)]
    public void GetPlainTextLength_Markup_ShouldCountVisibleCharactersOnly(string? html, int expected)
    {
        HtmlContentSanitizer.GetPlainTextLength(html).ShouldBe(expected);
    }

    [Fact]
    public void SanitizeBasicFormatting_Heading1_ShouldBeKept()
    {
        HtmlContentSanitizer.SanitizeBasicFormatting("<h1>Title</h1>")
            .ShouldNotBeNull()
            .ShouldContain("<h1");
    }

    [Fact]
    public void SanitizeBasicFormatting_MailtoLink_ShouldBeKept()
    {
        HtmlContentSanitizer.SanitizeBasicFormatting("<p><a href=\"mailto:a@b.example\">mail</a></p>")
            .ShouldNotBeNull()
            .ShouldContain("mailto:a@b.example");
    }

    [Fact]
    public void SanitizeBasicFormatting_ForeignStyleProperties_ShouldKeepOnlyTextAlign()
    {
        var result = HtmlContentSanitizer.SanitizeBasicFormatting(
            "<p style=\"position:fixed;text-align:center\">x</p>");

        result.ShouldNotBeNull();
        result.ShouldContain("text-align");
        result.ShouldNotContain("position");
    }

    [Fact]
    public void SanitizeBasicFormatting_LinkAttributes_ShouldKeepOnlyHref()
    {
        var result = HtmlContentSanitizer.SanitizeBasicFormatting(
            "<a href=\"https://a.example\" target=\"_blank\" rel=\"noopener\" class=\"c\" onclick=\"x()\">x</a>");

        result.ShouldNotBeNull();
        result.ShouldContain("href=\"https://a.example\"");
        result.ShouldNotContain("target");
        result.ShouldNotContain("rel=");
        result.ShouldNotContain("class");
        result.ShouldNotContain("onclick");
    }

    [Fact]
    public void SanitizeBasicFormatting_EncodedMarkup_ShouldStayEncoded()
    {
        HtmlContentSanitizer.SanitizeBasicFormatting("<p>&lt;script&gt;</p>")
            .ShouldNotBeNull()
            .ShouldContain("&lt;script&gt;");
    }

    // Mirrors the toolbar of src/web/cvitlo-app/src/app/shared/components/cv-rich-text/cv-rich-text.ts
    // (StarterKit + TextAlign + Link): bold, italic, underline, strike, lists, link, four alignments.
    [Theory]
    [InlineData("<p><strong>b</strong></p>", "<strong>")]
    [InlineData("<p><em>i</em></p>", "<em>")]
    [InlineData("<p><u>u</u></p>", "<u>")]
    [InlineData("<p><s>s</s></p>", "<s>")]
    [InlineData("<ul><li><p>item</p></li></ul>", "<ul>")]
    [InlineData("<ol><li><p>item</p></li></ol>", "<ol>")]
    [InlineData("<p><a target=\"_blank\" rel=\"noopener noreferrer nofollow\" class=\"x\" href=\"https://e.example\">l</a></p>", "href=\"https://e.example\"")]
    [InlineData("<p style=\"text-align: left\">t</p>", "text-align: left")]
    [InlineData("<p style=\"text-align: center\">t</p>", "text-align: center")]
    [InlineData("<p style=\"text-align: right\">t</p>", "text-align: right")]
    [InlineData("<p style=\"text-align: justify\">t</p>", "text-align: justify")]
    public void SanitizeBasicFormatting_TiptapEditorOutput_ShouldKeepEveryToolbarFormat(string input, string expected)
    {
        HtmlContentSanitizer.SanitizeBasicFormatting(input)
            .ShouldNotBeNull()
            .ShouldContain(expected);
    }
}
