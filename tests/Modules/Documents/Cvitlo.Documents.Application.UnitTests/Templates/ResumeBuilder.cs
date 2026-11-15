using Cvitlo.Documents.PublicApi.Resumes;

namespace Cvitlo.Documents.Application.UnitTests.Templates;

internal static class ResumeBuilder
{
    private static readonly DateOnly Start = new(2020, 1, 15);
    private static readonly DateOnly End = new(2021, 3, 20);

    public static ResumeDocument Full()
    {
        return new ResumeDocument
        {
            Localization = "en-us",
            Summary = "<p>Seasoned engineer</p>",
            Hobbies = "<p>Chess</p>",
            Person = FullPerson(),
            Links = [new LinkDocument { Label = "Portfolio", Uri = "https://portfolio.example" }],
            Skills = [new SkillDocument { Name = "Mathematics" }],
            Languages = [new LanguageDocument { Name = "English", Level = "C1" }],
            Experiences = [FullExperience()],
            Educations = [FullEducation()],
            Courses = [FullCourse()],
            Projects = [FullProject()]
        };
    }

    private static PersonDocument FullPerson()
    {
        return new PersonDocument
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

    private static ExperienceDocument FullExperience()
    {
        return new ExperienceDocument
        {
            JobTitle = "Backend Engineer",
            Employer = "Acme",
            City = "Lviv",
            StartDate = Start,
            EndDate = End,
            Description = "<p>Built engines</p>"
        };
    }

    private static EducationDocument FullEducation()
    {
        return new EducationDocument
        {
            School = "Polytechnic",
            Degree = "MSc",
            City = "Odesa",
            StartDate = Start,
            EndDate = End,
            Description = "<p>Thesis work</p>"
        };
    }

    private static CourseDocument FullCourse()
    {
        return new CourseDocument
        {
            Name = "Distributed Systems",
            School = "OpenCourse",
            StartDate = Start,
            EndDate = End,
            Description = "<p>Consensus</p>"
        };
    }

    private static ProjectDocument FullProject()
    {
        return new ProjectDocument
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
