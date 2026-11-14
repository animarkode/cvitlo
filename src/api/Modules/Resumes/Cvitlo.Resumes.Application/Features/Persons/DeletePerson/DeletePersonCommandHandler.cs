using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Persons.DeletePerson;

public class DeletePersonCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<DeletePersonCommand, ErrorOr<bool>>
{
    public async Task<ErrorOr<bool>> ExecuteAsync(DeletePersonCommand command, CancellationToken ct)
    {
        var person = await dbContext.Persons
            .FirstOrDefaultAsync(p => p.Id == command.Id && p.ResumeId == command.ResumeId, ct);

        if (person is null)
        {
            return Error.NotFound("Person.NotFound", $"Person with id '{command.Id}' was not found");
        }

        dbContext.Persons.Remove(person);
        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}