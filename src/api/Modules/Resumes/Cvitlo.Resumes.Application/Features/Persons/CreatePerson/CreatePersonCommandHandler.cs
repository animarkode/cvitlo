using ErrorOr;
using Cvitlo.Resumes.Application.Features.Persons;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Persons.CreatePerson;

public class CreatePersonCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<CreatePersonCommand, ErrorOr<CreatePersonResponse>>
{
    public async Task<ErrorOr<CreatePersonResponse>> ExecuteAsync(CreatePersonCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var personExists = await dbContext.Persons
            .AnyAsync(p => p.ResumeId == command.ResumeId, ct);

        if (personExists)
        {
            return Error.Conflict("Person.Conflict", "A person profile already exists for this resume.");
        }

        var person = new Domain.Entities.Person
        {
            Id = Guid.NewGuid(),
            ResumeId = command.ResumeId,
            JobTitle = command.JobTitle,
            Firstname = command.Firstname,
            Lastname = command.Lastname,
            Email = command.Email,
            Phone = command.Phone,
            Country = command.Country,
            City = command.City,
            Street = command.Street,
            PostCode = command.PostCode,
            Nationality = command.Nationality,
            DateOfBirth = command.DateOfBirth,
            DrivingLicense = command.DrivingLicense
        };

        if (resume.Title is null)
        {
            var title = ResumeTitleSnapshot.FromName(command.Firstname, command.Lastname);
            if (title is not null)
            {
                resume.Title = title;
            }
        }

        dbContext.Persons.Add(person);
        await dbContext.SaveChangesAsync(ct);

        return person.MapToResponse();
    }
}
