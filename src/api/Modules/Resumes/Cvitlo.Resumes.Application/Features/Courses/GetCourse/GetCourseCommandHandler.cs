using ErrorOr;
using Cvitlo.Resumes.Infrastructure.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace Cvitlo.Resumes.Application.Features.Courses.GetCourse;

public class GetCourseCommandHandler(ResumesDbContext dbContext)
    : ICommandHandler<GetCourseCommand, ErrorOr<GetCourseResponse>>
{
    public async Task<ErrorOr<GetCourseResponse>> ExecuteAsync(GetCourseCommand command, CancellationToken ct)
    {
        var course = await dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.Id && c.ResumeId == command.ResumeId, ct);

        if (course is null)
        {
            return Error.NotFound("Course.NotFound", $"Course with id '{command.Id}' was not found");
        }

        return course.MapToResponse();
    }
}