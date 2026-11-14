using Cvitlo.Resumes.Domain.Entities;

namespace Cvitlo.Documents.Application.UnitTests.Templates;

internal static class ResumeBuilder
{
    private static readonly DateOnly Start = new(2020, 1, 15);
    private static readonly DateOnly End = new(2021, 3, 20);

    public static Resume Full()
    {
        return new Resume
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Localization = "en-us",
            Summary = "<p>Seasoned engineer</p>",
            Hobbies = "<p>Chess</p>",
            Person = FullPerson(),
            Links = [new Link { Label = "Portfolio", Uri = "https://portfolio.example" }],
            Skills = [new Skill { Name = "Mathematics", Level = "Expert" }],
            Languages = [new Language { Name = "English", Level = "C1" }],
            Experiences = [FullExperience()],
            Educations = [FullEducation()],
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

    private static Experience FullExperience()
    {
        return new Experience
        {
            JobTitle = "Backend Engineer",
            Employer = "Acme",
            City = "Lviv",
            StartDate = Start,
            EndDate = End,
            Description = "<p>Built engines</p>"
        };
    }

    private static Education FullEducation()
    {
        return new Education
        {
            School = "Polytechnic",
            Degree = "MSc",
            City = "Odesa",
            StartDate = Start,
            EndDate = End,
            Description = "<p>Thesis work</p>"
        };
    }

    private static Course FullCourse()
    {
        return new Course
        {
            Name = "Distributed Systems",
            School = "OpenCourse",
            StartDate = Start,
            EndDate = End,
            Description = "<p>Consensus</p>"
        };
    }

    private static Project FullProject()
    {
        return new Project
        {
            Name = "Cvitlo",
            Tagline = "Resume builder",
            Url = "https://cvitlo.example",
            StartDate = Start,
            EndDate = End,
            Description = "<p>Open source</p>"
        };
    }
}
