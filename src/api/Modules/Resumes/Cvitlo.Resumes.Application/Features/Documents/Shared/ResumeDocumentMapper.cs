using Cvitlo.Documents.PublicApi.Resumes;
using Cvitlo.Resumes.Domain.Entities;

namespace Cvitlo.Resumes.Application.Features.Documents.Shared;

public static class ResumeDocumentMapper
{
    public static ResumeDocument ToDocument(this Resume resume)
    {
        return new ResumeDocument
        {
            Localization = resume.Localization,
            Summary = resume.Summary,
            Hobbies = resume.Hobbies,
            Person = resume.Person is null ? null : ToDocument(resume.Person),
            Experiences = resume.Experiences.Select(e => new ExperienceDocument
            {
                JobTitle = e.JobTitle,
                Employer = e.Employer,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                City = e.City,
                Description = e.Description
            }).ToList(),
            Educations = resume.Educations.Select(e => new EducationDocument
            {
                School = e.School,
                Degree = e.Degree,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                City = e.City,
                Description = e.Description
            }).ToList(),
            Links = resume.Links.Select(l => new LinkDocument { Label = l.Label, Uri = l.Uri }).ToList(),
            Skills = resume.Skills.Select(s => new SkillDocument { Name = s.Name }).ToList(),
            Languages = resume.Languages.Select(l => new LanguageDocument { Name = l.Name, Level = l.Level }).ToList(),
            Courses = resume.Courses.Select(c => new CourseDocument
            {
                School = c.School,
                Name = c.Name,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Description = c.Description
            }).ToList(),
            Projects = resume.Projects.Select(p => new ProjectDocument
            {
                Name = p.Name,
                Tagline = p.Tagline,
                Url = p.Url,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Description = p.Description
            }).ToList()
        };
    }

    private static PersonDocument ToDocument(Person person)
    {
        return new PersonDocument
        {
            JobTitle = person.JobTitle,
            Firstname = person.Firstname,
            Lastname = person.Lastname,
            Email = person.Email,
            Phone = person.Phone,
            Country = person.Country,
            City = person.City,
            Street = person.Street,
            PostCode = person.PostCode,
            Nationality = person.Nationality,
            DateOfBirth = person.DateOfBirth,
            DrivingLicense = person.DrivingLicense
        };
    }
}
