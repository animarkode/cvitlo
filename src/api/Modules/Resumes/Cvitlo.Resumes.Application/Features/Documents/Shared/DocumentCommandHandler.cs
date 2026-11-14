using ErrorOr;
using Cvitlo.Resumes.Domain.Entities;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Documents.Shared;

public class DocumentCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<DocumentCommand, ErrorOr<Resume>>
{
    public async Task<ErrorOr<Resume>> ExecuteAsync(
        DocumentCommand command,
        CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .AsNoTracking()
            .Include(r => r.Person)
            .Include(r => r.Experiences.OrderBy(e => e.Order))
            .Include(r => r.Educations.OrderBy(e => e.Order))
            .Include(r => r.Skills.OrderBy(s => s.Order))
            .Include(r => r.Languages.OrderBy(l => l.Order))
            .Include(r => r.Courses.OrderBy(c => c.Order))
            .Include(r => r.Projects.OrderBy(p => p.Order))
            .Include(r => r.Links.OrderBy(l => l.Order))
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId && r.UserId == command.UserId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        return resume;
    }
}
