using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Persons.GetPerson;

public class GetPersonCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetPersonCommand, ErrorOr<GetPersonResponse>>
{
    public async Task<ErrorOr<GetPersonResponse>> ExecuteAsync(GetPersonCommand command, CancellationToken ct)
    {
        var person = await dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == command.Id && p.ResumeId == command.ResumeId, ct);

        if (person is null)
        {
            return Error.NotFound("Person.NotFound", $"Person with id '{command.Id}' was not found");
        }

        return person.MapToResponse();
    }
}