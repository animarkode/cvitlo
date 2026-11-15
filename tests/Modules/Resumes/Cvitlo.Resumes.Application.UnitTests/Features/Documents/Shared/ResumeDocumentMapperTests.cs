using Cvitlo.Resumes.Application.Features.Documents.Shared;
using Cvitlo.Resumes.Domain.Entities;
using Shouldly;

namespace Cvitlo.Resumes.Application.UnitTests.Features.Documents.Shared;

public class ResumeDocumentMapperTests
{
    private static readonly DateOnly Start = new(2020, 1, 2);
    private static readonly DateOnly End = new(2021, 3, 4);

    [Fact]
    public void ToDocument_FilledResume_ShouldMapScalarFields()
    {
        var document = FilledResume().ToDocument();

        document.Localization.ShouldBe("en");
        document.Summary.ShouldBe("<p>summary</p>");
        document.Hobbies.ShouldBe("<p>hobbies</p>");
    }

    [Fact]
    public void ToDocument_FilledResume_ShouldMapPerson()
    {
        var document = FilledResume().ToDocument();

        document.Person.ShouldNotBeNull();
        document.Person.JobTitle.ShouldBe("Engineer");
        document.Person.Firstname.ShouldBe("Ada");
        document.Person.Lastname.ShouldBe("Lovelace");
        document.Person.Email.ShouldBe("ada@example.com");
        document.Person.Phone.ShouldBe("123");
        document.Person.Country.ShouldBe("UK");
        document.Person.City.ShouldBe("London");
        document.Person.Street.ShouldBe("Main 1");
        document.Person.PostCode.ShouldBe("N1");
        document.Person.Nationality.ShouldBe("British");
        document.Person.DateOfBirth.ShouldBe(new DateOnly(1815, 12, 10));
        document.Person.DrivingLicense.ShouldBe("B");
    }

    [Fact]
    public void ToDocument_FilledResume_ShouldMapEverySectionEntry()
    {
        var document = FilledResume().ToDocument();

        var experience = document.Experiences.ShouldHaveSingleItem();
        experience.JobTitle.ShouldBe("Dev");
        experience.Employer.ShouldBe("Acme");
        experience.StartDate.ShouldBe(Start);
        experience.EndDate.ShouldBe(End);
        experience.City.ShouldBe("Kyiv");
        experience.Description.ShouldBe("exp");

        var education = document.Educations.ShouldHaveSingleItem();
        education.School.ShouldBe("Uni");
        education.Degree.ShouldBe("BSc");
        education.StartDate.ShouldBe(Start);
        education.EndDate.ShouldBe(End);
        education.City.ShouldBe("Lviv");
        education.Description.ShouldBe("edu");

        var link = document.Links.ShouldHaveSingleItem();
        link.Label.ShouldBe("Site");
        link.Uri.ShouldBe("https://example.com");

        document.Skills.ShouldHaveSingleItem().Name.ShouldBe("C#");

        var language = document.Languages.ShouldHaveSingleItem();
        language.Name.ShouldBe("English");
        language.Level.ShouldBe("C1");

        var course = document.Courses.ShouldHaveSingleItem();
        course.School.ShouldBe("Academy");
        course.Name.ShouldBe("Course");
        course.StartDate.ShouldBe(Start);
        course.EndDate.ShouldBe(End);
        course.Description.ShouldBe("crs");

        var project = document.Projects.ShouldHaveSingleItem();
        project.Name.ShouldBe("Proj");
        project.Tagline.ShouldBe("Tag");
        project.Url.ShouldBe("https://p.example.com");
        project.StartDate.ShouldBe(Start);
        project.EndDate.ShouldBe(End);
        project.Description.ShouldBe("prj");
    }

    [Fact]
    public void ToDocument_MultipleItems_ShouldKeepCollectionOrder()
    {
        var resume = new Resume
        {
            Skills = [new Skill { Name = "B" }, new Skill { Name = "A" }, new Skill { Name = "C" }],
            Experiences = [new Experience { JobTitle = "2" }, new Experience { JobTitle = "1" }]
        };

        var document = resume.ToDocument();

        document.Skills.Select(s => s.Name).ShouldBe(["B", "A", "C"]);
        document.Experiences.Select(e => e.JobTitle).ShouldBe(["2", "1"]);
    }

    [Fact]
    public void ToDocument_NoPerson_ShouldMapPersonToNull()
    {
        var document = new Resume { Person = null }.ToDocument();

        document.Person.ShouldBeNull();
    }

    private static Resume FilledResume()
    {
        return new Resume
        {
            Localization = "en",
            Summary = "<p>summary</p>",
            Hobbies = "<p>hobbies</p>",
            Person = new Person
            {
                JobTitle = "Engineer",
                Firstname = "Ada",
                Lastname = "Lovelace",
                Email = "ada@example.com",
                Phone = "123",
                Country = "UK",
                City = "London",
                Street = "Main 1",
                PostCode = "N1",
                Nationality = "British",
                DateOfBirth = new DateOnly(1815, 12, 10),
                DrivingLicense = "B"
            },
            Experiences = [new Experience { JobTitle = "Dev", Employer = "Acme", StartDate = Start, EndDate = End, City = "Kyiv", Description = "exp" }],
            Educations = [new Education { School = "Uni", Degree = "BSc", StartDate = Start, EndDate = End, City = "Lviv", Description = "edu" }],
            Links = [new Link { Label = "Site", Uri = "https://example.com" }],
            Skills = [new Skill { Name = "C#", Level = "Expert" }],
            Languages = [new Language { Name = "English", Level = "C1" }],
            Courses = [new Course { School = "Academy", Name = "Course", StartDate = Start, EndDate = End, Description = "crs" }],
            Projects = [new Project { Name = "Proj", Tagline = "Tag", Url = "https://p.example.com", StartDate = Start, EndDate = End, Description = "prj" }]
        };
    }
}
