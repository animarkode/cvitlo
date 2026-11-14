using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Courses.GetAllCourses;

public class GetAllCoursesCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetAllCoursesCommand, ErrorOr<GetAllCoursesResponse>>
{
    public async Task<ErrorOr<GetAllCoursesResponse>> ExecuteAsync(GetAllCoursesCommand command, CancellationToken ct)
    {
        var resume = await dbContext.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.ResumeId, ct);

        if (resume is null)
        {
            return Error.NotFound("Resume.NotFound", $"Resume with id '{command.ResumeId}' was not found");
        }

        var courses = await dbContext.Courses
            .AsNoTracking()
            .Where(c => c.ResumeId == command.ResumeId)
            .OrderBy(c => c.Order)
            .ToListAsync(ct);

        var items = courses.Select(e => e.MapToItem()).ToList();

        return new GetAllCoursesResponse(items);
    }
}