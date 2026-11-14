using Cvitlo.Documents.Application.Razor.Extensions;
using Shouldly;

namespace Cvitlo.Documents.Application.UnitTests.Razor.Extensions;

public class HtmlSanitizerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Sanitize_EmptyOrWhitespace_ShouldReturnEmpty(string input)
    {
        input.Sanitize().ShouldBe(string.Empty);
    }

    [Fact]
    public void Sanitize_NonBreakingSpaces_ShouldBecomeRegularSpaces()
    {
        "a\u00A0b\u202Fc\u2007d&nbsp;e".Sanitize().ShouldBe("a b c d e");
    }

    [Fact]
    public void Sanitize_InvisibleCharacters_ShouldBeRemoved()
    {
        "a\u200Bb\u200Cc\u200Dd\uFEFFe\u2060f\u00ADg&shy;h".Sanitize().ShouldBe("abcdefgh");
    }

    [Fact]
    public void Sanitize_RepeatedSpaces_ShouldCollapseAndTrim()
    {
        "  a   b  ".Sanitize().ShouldBe("a b");
    }

    [Fact]
    public void Sanitize_TiptapMarkup_ShouldStayUnchanged()
    {
        const string markup = "<p><strong>a</strong> <em>b</em></p>";

        markup.Sanitize().ShouldBe(markup);
    }

    [Fact(Skip = "#152: control-character regex is Java syntax; strips '^]' sequences")]
    public void Sanitize_BracketsAndAmpersands_ShouldBeKept()
    {
        const string text = "a[]b &] c^] x";

        text.Sanitize().ShouldBe(text);
    }

    [Fact(Skip = "#152: control-character regex is Java syntax; control chars survive")]
    public void Sanitize_ControlCharacters_ShouldBeRemovedExceptTabAndNewlines()
    {
        "a\u0001b\u0007c\td\ne".Sanitize().ShouldBe("abc\td\ne");
    }

    [Fact(Skip = "#144: HtmlDecode re-activates encoded markup into live tags")]
    public void Sanitize_EncodedMarkup_ShouldNotBecomeLiveMarkup()
    {
        var result = "<p>&lt;script&gt;alert(1)&lt;/script&gt;</p>".Sanitize();

        result.ShouldNotContain("<script");
    }

    [Fact]
    public void Sanitize_OneMegabyteInput_ShouldNotTimeOut()
    {
        var input = string.Concat(Enumerable.Repeat("word  ", 170_000));

        Should.NotThrow(() => input.Sanitize());
    }
}
