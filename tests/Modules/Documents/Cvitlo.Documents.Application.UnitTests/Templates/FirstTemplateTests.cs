using System.Globalization;
using Cvitlo.Resumes.Domain.Entities;
using Shouldly;

namespace Cvitlo.Documents.Application.UnitTests.Templates;

public class FirstTemplateTests
{
    private const string Separator = "white-space-pre\">, <";

    [Fact]
    public async Task Render_NullResume_ShouldRenderHeaderOnly()
    {
        var html = await TemplateRenderer.RenderAsync(null);

        AssertHeaderOnly(html);
    }

    [Fact]
    public async Task Render_EmptyResume_ShouldOmitEverySection()
    {
        var html = await TemplateRenderer.RenderAsync(new Resume());

        AssertHeaderOnly(html);
    }

    [Fact]
    public async Task Render_FullResume_ShouldRenderEverySection()
    {
        var html = await TemplateRenderer.RenderAsync(ResumeBuilder.Full());

        string[] expected =
        [
            "Summary", "Employment History", "Education", "Courses", "Projects", "Skills", "Languages",
            "Hobbies", "Nationality", "Date of birth", "Driving license",
            "Seasoned engineer", "Chess", "Ada", "Lovelace", "Analyst", "ada@example.com", "0501234567",
            "Ukraine", "Kyiv", "Main Street 1", "01001", "Ukrainian", "07.03.1990", "Category B",
            "Portfolio", "https://portfolio.example", "Mathematics", "English", "C1",
            "Backend Engineer", "Acme", "Lviv", "Built engines", "Polytechnic", "MSc", "Odesa", "Thesis work",
            "Distributed Systems", "OpenCourse", "Consensus", "Cvitlo", "Resume builder",
            "https://cvitlo.example", "Open source", "January 2020", "March 2021"
        ];

        foreach (var value in expected)
        {
            html.ShouldContain(value);
        }
    }

    [Fact]
    public async Task Render_WhitespaceOnlyOptionalFields_ShouldOmitTheirSections()
    {
        var resume = new Resume
        {
            Localization = "en-us",
            Summary = "  ",
            Hobbies = "  ",
            Person = new Person { Nationality = "  ", DrivingLicense = "  " }
        };

        var html = await TemplateRenderer.RenderAsync(resume);

        html.ShouldNotContain("Nationality");
        html.ShouldNotContain("Driving license");
        html.ShouldNotContain("Hobbies");
        html.ShouldNotContain("Summary");
    }

    [Fact]
    public async Task Render_MarkupInPlainFields_ShouldBeEncoded()
    {
        const string payload = "<script>x</script>";
        var resume = new Resume
        {
            Localization = "en-us",
            Person = new Person { Firstname = payload, JobTitle = payload },
            Skills = [new Skill { Name = payload }],
            Links = [new Link { Label = payload, Uri = "https://a.example" }]
        };

        var html = await TemplateRenderer.RenderAsync(resume);

        html.ShouldNotContain("<script>");
        html.ShouldContain("&lt;script&gt;");
    }

    [Fact(Skip = "test-audit 03-B1: render-time Sanitize decodes encoded markup into a live <img> tag")]
    public async Task Render_SummaryWithEncodedMarkup_ShouldNotEmitLiveTags()
    {
        var resume = new Resume
        {
            Localization = "en-us",
            Summary = "<p>&lt;img src=x onerror=alert(1)&gt;</p>"
        };

        var html = await TemplateRenderer.RenderAsync(resume);

        html.ShouldNotContain("<img");
    }

    [Theory]
    [InlineData("https://a.example")]
    [InlineData("http://a.example")]
    public async Task Render_ProjectUrl_ShouldLinkHttpAndHttps(string url)
    {
        var html = await RenderProjectUrlAsync(url);

        html.ShouldContain($"href=\"{url}\"");
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://a.example")]
    [InlineData("/relative")]
    [InlineData("")]
    public async Task Render_ProjectUrl_ShouldNotLinkOtherSchemes(string url)
    {
        var html = await RenderProjectUrlAsync(url);

        html.ShouldNotContain($"href=\"{url}\"");
    }

    [Fact(Skip = "test-audit 03-B5: link Uri is emitted as href without a scheme check")]
    public async Task Render_LinkWithJavascriptUri_ShouldNotEmitItAsHref()
    {
        var resume = new Resume
        {
            Localization = "en-us",
            Links = [new Link { Label = "x", Uri = "javascript:alert(1)" }]
        };

        var html = await TemplateRenderer.RenderAsync(resume);

        html.ShouldNotContain("href=\"javascript:");
    }

    public static TheoryData<Person, int> ContactCases => new()
    {
        { new Person { City = "Kyiv" }, 0 },
        { new Person { City = "Kyiv", Email = "ada@example.com" }, 1 },
        {
            new Person
            {
                City = "Kyiv",
                Street = "Main Street 1",
                PostCode = "01001",
                Country = "Ukraine",
                Phone = "0501234567",
                Email = "ada@example.com"
            },
            5
        }
    };

    [Theory]
    [MemberData(nameof(ContactCases))]
    public async Task Render_Contacts_ShouldSeparateOnlyBetweenPresentValues(Person person, int separators)
    {
        var html = await TemplateRenderer.RenderAsync(new Resume { Localization = "en-us", Person = person });

        CountOccurrences(html, Separator).ShouldBe(separators);
    }

    [Fact(Skip = "test-audit 03-B6: IsComma compares values, so City equal to Country loses its separator")]
    public async Task Render_CityEqualToCountry_ShouldStillSeparateThem()
    {
        var person = new Person { City = "Monaco", Country = "Monaco" };

        var html = await TemplateRenderer.RenderAsync(new Resume { Localization = "en-us", Person = person });

        CountOccurrences(html, Separator).ShouldBe(1);
    }

    [Theory]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(true, true, 1)]
    [InlineData(false, false, 0)]
    public async Task Render_DateInterval_ShouldRenderPresentEnds(bool hasStart, bool hasEnd, int dashes)
    {
        var resume = new Resume
        {
            Localization = "en-us",
            Experiences =
            [
                new Experience
                {
                    JobTitle = "Dev",
                    StartDate = hasStart ? new DateOnly(2020, 1, 15) : null,
                    EndDate = hasEnd ? new DateOnly(2021, 3, 20) : null
                }
            ]
        };

        var html = await TemplateRenderer.RenderAsync(resume);

        CountOccurrences(html, "<span> - </span>").ShouldBe(dashes);
        AssertPresence(html, "January 2020", hasStart);
        AssertPresence(html, "March 2021", hasEnd);
    }

    [Fact]
    public async Task Render_DateOfBirth_ShouldUseDayMonthYear()
    {
        var resume = new Resume
        {
            Localization = "en-us",
            Person = new Person { DateOfBirth = new DateOnly(1990, 3, 7) }
        };

        var html = await TemplateRenderer.RenderAsync(resume);

        html.ShouldContain("07.03.1990");
    }

    [Theory]
    [InlineData("en-us", "Skills")]
    [InlineData("uk-ua", "Вміння та навички")]
    [InlineData("uk", "Вміння та навички")]
    [InlineData("pl-pl", "Umiejętności")]
    [InlineData("pl", "Umiejętności")]
    [InlineData(null, "Skills")]
    [InlineData("de-de", "Skills")]
    public async Task Render_Localization_ShouldTranslateHeadings(string? localization, string heading)
    {
        var resume = new Resume
        {
            Localization = localization,
            Skills = [new Skill { Name = "Mathematics" }]
        };

        var html = await TemplateRenderer.RenderAsync(resume);

        System.Net.WebUtility.HtmlDecode(html).ShouldContain(heading);
    }

    [Fact(Skip = "test-audit 03-B3: unknown localization makes CultureInfo.GetCultureInfo throw CultureNotFoundException")]
    public async Task Render_UnknownLocalization_ShouldFallBackToEnglish()
    {
        var resume = new Resume
        {
            Localization = "not a culture!",
            Skills = [new Skill { Name = "Mathematics" }]
        };

        var html = await TemplateRenderer.RenderAsync(resume);

        html.ShouldContain("Skills");
    }

    [Fact]
    public async Task Render_TwoCulturesInARow_ShouldNotLeakCulture()
    {
        var beforeUi = CultureInfo.CurrentUICulture;
        var before = CultureInfo.CurrentCulture;
        var skills = new[] { new Skill { Name = "Mathematics" } };

        await TemplateRenderer.RenderAsync(new Resume { Localization = "uk-ua", Skills = skills });
        await TemplateRenderer.RenderAsync(new Resume { Localization = "en-us", Skills = skills });

        CultureInfo.CurrentUICulture.ShouldBe(beforeUi);
        CultureInfo.CurrentCulture.ShouldBe(before);
    }

    [Fact]
    public async Task Render_LargeResume_ShouldRenderEveryItem()
    {
        var description = new string('x', 1000);
        var resume = new Resume
        {
            Localization = "en-us",
            Experiences = Enumerable.Range(0, 200)
                .Select(i => new Experience { JobTitle = $"Last-{i}", Description = description })
                .ToList()
        };

        var html = await TemplateRenderer.RenderAsync(resume);

        html.ShouldContain("Last-199");
    }

    [Fact]
    public async Task Render_DescriptionMarkup_ShouldBeEmittedAsMarkup()
    {
        const string description = "<p><strong>x</strong></p>";
        var resume = new Resume
        {
            Localization = "en-us",
            Experiences = [new Experience { Description = description }],
            Educations = [new Education { Description = description }],
            Courses = [new Course { Description = description }]
        };

        var html = await TemplateRenderer.RenderAsync(resume);

        CountOccurrences(html, "<strong>x</strong>").ShouldBe(3);
    }

    private static async Task<string> RenderProjectUrlAsync(string url)
    {
        var resume = new Resume
        {
            Localization = "en-us",
            Projects = [new Project { Name = "P", Url = url }]
        };

        return await TemplateRenderer.RenderAsync(resume);
    }

    private static void AssertPresence(string html, string value, bool expected)
    {
        if (expected)
        {
            html.ShouldContain(value);
            return;
        }

        html.ShouldNotContain(value);
    }

    private static void AssertHeaderOnly(string html)
    {
        html.ShouldContain("class=\"header\"");
        html.ShouldNotContain("<div class=\"section-left\">");
        html.ShouldNotContain("<div class=\"section-right\">");
    }

    private static int CountOccurrences(string text, string value)
    {
        return text.Split(value, StringSplitOptions.None).Length - 1;
    }
}
