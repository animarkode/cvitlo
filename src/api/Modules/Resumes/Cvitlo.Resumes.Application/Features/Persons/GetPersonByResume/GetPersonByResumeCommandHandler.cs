using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Persons.GetPersonByResume;

public class GetPersonByResumeCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetPersonByResumeCommand, ErrorOr<GetPersonByResumeResponse>>
{
    public async Task<ErrorOr<GetPersonByResumeResponse>> ExecuteAsync(
        GetPersonByResumeCommand command,
        CancellationToken ct)
    {
        var person = await dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ResumeId == command.ResumeId, ct);

        if (person is null)
        {
            return Error.NotFound("Person.NotFound", $"Person for resume '{command.ResumeId}' was not found");
        }

        return person.MapToResponse();
    }
}