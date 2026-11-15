using Cvitlo.Documents.Application;
using Cvitlo.Documents.Application.Templates;
using Cvitlo.Documents.PublicApi;
using Cvitlo.Resumes.Application.Features.Documents.Shared;
using Cvitlo.Resumes.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cvitlo.Resumes.Application.UnitTests.Features.Documents.Shared;

// The mapper tests check fields one by one; this one proves a stored resume still reaches the real template intact.
public class ResumeDocumentRenderTests
{
    private static readonly DateOnly Start = new(2020, 1, 15);
    private static readonly DateOnly End = new(2021, 3, 20);

    [Fact]
    public async Task RenderFirstTemplate_FullDomainResume_ShouldRenderEveryMappedValue()
    {
        await using var provider = new ServiceCollection().AddLogging().AddDocumentsModule().BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var api = scope.ServiceProvider.GetRequiredService<IDocumentsModuleApi>();
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal) { ["Resume"] = FullResume().ToDocument() };

        var html = await api.RenderToHtmlAsync<FirstTemplate>(parameters, TestContext.Current.CancellationToken);

        string[] expected =
        [
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

        html.ShouldContain("MSc, Polytechnic, Odesa");
        html.ShouldContain("Distributed Systems, OpenCourse");
    }

    private static Resume FullResume()
    {
        return new Resume
        {
            Id = Guid.NewGuid(),
            Localization = "en-us",
            Summary = "<p>Seasoned engineer</p>",
            Hobbies = "<p>Chess</p>",
            Person = FullPerson(),
            Links = [new Link { Label = "Portfolio", Uri = "https://portfolio.example" }],
            Skills = [new Skill { Name = "Mathematics", Level = "Expert" }],
            Languages = [new Language { Name = "English", Level = "C1" }],
            Experiences =
            [
                new Experience
                {
                    JobTitle = "Backend Engineer", Employer = "Acme", City = "Lviv",
                    StartDate = Start, EndDate = End, Description = "<p>Built engines</p>"
                }
            ],
            Educations =
            [
                new Education
                {
                    School = "Polytechnic", Degree = "MSc", City = "Odesa",
                    StartDate = Start, EndDate = End, Description = "<p>Thesis work</p>"
                }
            ],
            Courses = [FullCourse()],
            Projects = [FullProject()]
        };
    }

    private static Person FullPerson()
    {
        return new Person
        {
            Firstname = "Ada",
            Lastname = "Lovelace",
            JobTitle = "Analyst",
            Email = "ada@example.com",
            Phone = "0501234567",
            Country = "Ukraine",
            City = "Kyiv",
            Street = "Main Street 1",
            PostCode = "01001",
            Nationality = "Ukrainian",
            DateOfBirth = new DateOnly(1990, 3, 7),
            DrivingLicense = "Category B"
        };
    }

    private static Course FullCourse()
    {
        return new Course
        {
            Name = "Distributed Systems", School = "OpenCourse",
            StartDate = Start, EndDate = End, Description = "<p>Consensus</p>"
        };
    }

    private static Project FullProject()
    {
        return new Project
        {
            Name = "Cvitlo", Tagline = "Resume builder", Url = "https://cvitlo.example",
            StartDate = Start, EndDate = End, Description = "<p>Open source</p>"
        };
    }
}
